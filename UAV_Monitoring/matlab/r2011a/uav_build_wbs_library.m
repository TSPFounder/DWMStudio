function libFile = uav_build_wbs_library(outDir, wbsFile)
%UAV_BUILD_WBS_LIBRARY  Build the Simulink library of WBS subsystems (R2011a).
%   LIBFILE = UAV_BUILD_WBS_LIBRARY(OUTDIR, WBSFILE) creates uav_wbs_lib.mdl
%   in OUTDIR. WBSFILE is baseline/wbs_elements.tsv. Every row becomes a
%   Subsystem block named 'WBS <code> <name>', nested by WBS level, with the
%   WBS code in its Tag. Level-3 elements carry the behavior; level-2
%   elements wire their children and expose the interface.
%
%   The model uses only Inport, Outport, Subsystem, Fcn, Mux, Integrator,
%   Gain and Transport Delay blocks with numeric parameters written into the
%   blocks, so it needs no workspace variables and opens in later releases.
%
%   A row in WBSFILE with no behavior defined here is an error, and so is a
%   behavior defined here with no row: the WBS and the model stay in step.

p = uav_params();
wbs = uav_read_tsv(wbsFile);
lib = 'uav_wbs_lib';
if bdIsLoaded(lib)
    close_system(lib, 0);
end
new_system(lib, 'Library');

built = {};
levels = str2double(wbs.Level);
for k = find(levels == 2)'
    code = wbs.Code{k};
    name = ['WBS ' code ' ' wbs.Name{k}];
    switch code
        case '1.1'
            sys = uav_subsystem(lib, name, {'tgt_e', 'tgt_n', 'in_fov'}, ...
                {'pos_e', 'pos_n', 'link_ok', 'candidate', 'soc'}, ['WBS ' code], wbs.Description{k});
        case '1.2'
            sys = uav_subsystem(lib, name, {'pos_e', 'pos_n', 'tgt_e', 'tgt_n', 'candidate', 'auth'}, ...
                {'in_fov', 'alarm_on'}, ['WBS ' code], wbs.Description{k});
        case '1.3'
            sys = uav_subsystem(lib, name, {'auth_req', 'candidate', 'link_ok', 'fixed_det', 'in_fov'}, ...
                {'auth', 'event', 'notified', 'evidence_s'}, ['WBS ' code], wbs.Description{k});
        case '1.4'
            sys = uav_subsystem(lib, name, {'tgt_e', 'tgt_n', 'assist_req'}, ...
                {'fixed_det', 'assist'}, ['WBS ' code], wbs.Description{k});
        otherwise
            error('uav:wbs:unknown', 'No interface is defined for level-2 WBS element %s.', code);
    end
    built{end + 1} = code; %#ok<AGROW>
    children = find(levels == 3 & strncmp(wbs.Code, [code '.'], numel(code) + 1))';
    for c = children
        leaf(sys, wbs.Code{c}, wbs.Name{c}, wbs.Description{c}, p);
        built{end + 1} = wbs.Code{c}; %#ok<AGROW>
    end
    wire(sys, code);
end
missing = setdiff(wbs.Code, built);
if ~isempty(missing)
    error('uav:wbs:unbuilt', 'WBS element %s was not built.', missing{1});
end

if ~exist(outDir, 'dir')
    mkdir(outDir);
end
libFile = fullfile(outDir, [lib '.mdl']);
save_system(lib, libFile);
end

function leaf(parent, code, name, description, p)
% One level-3 WBS element: its ports and its behavior.
block = ['WBS ' code ' ' name];
tag = ['WBS ' code];
switch code
    case '1.1.1' % Airframe: point mass on a course angle theta, east/north position.
        s = uav_subsystem(parent, block, {'spd_cmd', 'turn_cmd'}, {'pos_e', 'pos_n', 'speed'}, tag, description);
        uav_add(s, 'Integrator', 'theta', 'InitialCondition', '0');
        add_line(s, 'turn_cmd/1', 'theta/1', 'autorouting', 'on');
        ve = uav_fcn(s, 'vel_e', '-u(1)*sin(u(2))', {'spd_cmd/1', 'theta/1'});
        vn = uav_fcn(s, 'vel_n', 'u(1)*cos(u(2))', {'spd_cmd/1', 'theta/1'});
        uav_add(s, 'Integrator', 'east', 'InitialCondition', uav_num(p.patrol_radius_m));
        uav_add(s, 'Integrator', 'north', 'InitialCondition', '0');
        add_line(s, ve, 'east/1', 'autorouting', 'on');
        add_line(s, vn, 'north/1', 'autorouting', 'on');
        add_line(s, 'east/1', 'pos_e/1', 'autorouting', 'on');
        add_line(s, 'north/1', 'pos_n/1', 'autorouting', 'on');
        add_line(s, 'spd_cmd/1', 'speed/1', 'autorouting', 'on');
    case '1.1.2' % Propulsion: electrical power = hover power + k * speed^3.
        s = uav_subsystem(parent, block, {'speed'}, {'power_w'}, tag, description);
        out = uav_fcn(s, 'power', [uav_num(p.hover_power_w) ' + ' uav_num(p.drag_power_coeff_w_per_mps3) '*u^3'], {'speed/1'});
        add_line(s, out, 'power_w/1', 'autorouting', 'on');
    case '1.1.3' % Electrical power: state of charge and reserve flag.
        s = uav_subsystem(parent, block, {'power_w'}, {'soc', 'soc_low'}, tag, description);
        uav_add(s, 'Gain', 'per Wh', 'Gain', uav_num(1 / (3600 * p.usable_energy_wh)));
        uav_add(s, 'Integrator', 'used', 'InitialCondition', '0');
        add_line(s, 'power_w/1', 'per Wh/1', 'autorouting', 'on');
        add_line(s, 'per Wh/1', 'used/1', 'autorouting', 'on');
        soc = uav_fcn(s, 'remaining', '1 - u', {'used/1'});
        low = uav_fcn(s, 'below reserve', ['u < ' uav_num(p.reserve_fraction)], {soc});
        add_line(s, soc, 'soc/1', 'autorouting', 'on');
        add_line(s, low, 'soc_low/1', 'autorouting', 'on');
    case '1.1.4' % Flight control: patrol speed until the reserve flag, left turn on the patrol circle.
        s = uav_subsystem(parent, block, {'soc_low'}, {'spd_cmd', 'turn_cmd'}, tag, description);
        spd = uav_fcn(s, 'speed command', [uav_num(p.patrol_speed_mps) '*(1 - u)'], {'soc_low/1'});
        uav_add(s, 'Gain', 'turn rate', 'Gain', uav_num(1 / p.patrol_radius_m));
        add_line(s, spd, 'turn rate/1', 'autorouting', 'on');
        add_line(s, spd, 'spd_cmd/1', 'autorouting', 'on');
        add_line(s, 'turn rate/1', 'turn_cmd/1', 'autorouting', 'on');
    case '1.1.5' % Communications: link available inside the link range of the station at the origin.
        s = uav_subsystem(parent, block, {'pos_e', 'pos_n'}, {'link_ok'}, tag, description);
        out = uav_fcn(s, 'in range', ['sqrt(u(1)^2 + u(2)^2) < ' uav_num(p.link_range_m)], {'pos_e/1', 'pos_n/1'});
        add_line(s, out, 'link_ok/1', 'autorouting', 'on');
    case '1.1.6' % Onboard processing: a target seen outside the enclosure is a candidate.
        s = uav_subsystem(parent, block, {'in_fov', 'tgt_e', 'tgt_n'}, {'candidate'}, tag, description);
        out = uav_fcn(s, 'outside enclosure', ['u(1)*(abs(u(2)) > ' uav_num(p.enclosure_half_width_m) ')'], ...
            {'in_fov/1', 'tgt_e/1', 'tgt_n/1'});
        add_line(s, out, 'candidate/1', 'autorouting', 'on');
    case '1.2.1' % EO camera: target inside the ground footprint under the aircraft.
        s = uav_subsystem(parent, block, {'pos_e', 'pos_n', 'tgt_e', 'tgt_n'}, {'in_fov'}, tag, description);
        out = uav_fcn(s, 'in footprint', ['sqrt((u(1) - u(3))^2 + (u(2) - u(4))^2) < ' uav_num(p.camera_footprint_radius_m)], ...
            {'pos_e/1', 'pos_n/1', 'tgt_e/1', 'tgt_n/1'});
        add_line(s, out, 'in_fov/1', 'autorouting', 'on');
    case '1.2.2' % Audible alarm: sounds only while a candidate exists and authorization is active.
        s = uav_subsystem(parent, block, {'candidate', 'auth'}, {'alarm_on'}, tag, description);
        out = uav_fcn(s, 'gated', 'u(1)*u(2)', {'candidate/1', 'auth/1'});
        add_line(s, out, 'alarm_on/1', 'autorouting', 'on');
    case '1.3.1' % Ground control station: operator authorization passes through.
        s = uav_subsystem(parent, block, {'auth_req'}, {'auth'}, tag, description);
        uav_add(s, 'Gain', 'authorize', 'Gain', '1');
        add_line(s, 'auth_req/1', 'authorize/1', 'autorouting', 'on');
        add_line(s, 'authorize/1', 'auth/1', 'autorouting', 'on');
    case '1.3.2' % Event manager: a linked UAV candidate or a fixed detection opens an event, which stays open.
        s = uav_subsystem(parent, block, {'candidate', 'link_ok', 'fixed_det'}, {'event'}, tag, description);
        any_ = uav_fcn(s, 'confirmed', '(u(1)*u(2) + u(3)) > 0', {'candidate/1', 'link_ok/1', 'fixed_det/1'});
        uav_add(s, 'Integrator', 'held', 'InitialCondition', '0');
        add_line(s, any_, 'held/1', 'autorouting', 'on');
        out = uav_fcn(s, 'open', 'u > 0', {'held/1'});
        add_line(s, out, 'event/1', 'autorouting', 'on');
    case '1.3.3' % Notification gateway: delivery reaches the recipient after the delivery delay.
        s = uav_subsystem(parent, block, {'event'}, {'notified'}, tag, description);
        uav_add(s, 'TransportDelay', 'delivery', 'DelayTime', uav_num(p.notify_delay_s), 'InitialOutput', '0');
        add_line(s, 'event/1', 'delivery/1', 'autorouting', 'on');
        add_line(s, 'delivery/1', 'notified/1', 'autorouting', 'on');
    case '1.3.4' % Evidence store: seconds of imagery with the target in view.
        s = uav_subsystem(parent, block, {'in_fov'}, {'evidence_s'}, tag, description);
        uav_add(s, 'Integrator', 'recorded', 'InitialCondition', '0');
        add_line(s, 'in_fov/1', 'recorded/1', 'autorouting', 'on');
        add_line(s, 'recorded/1', 'evidence_s/1', 'autorouting', 'on');
    case '1.4.1' % Fixed camera node: target inside the node's radius.
        s = uav_subsystem(parent, block, {'tgt_e', 'tgt_n'}, {'fixed_det'}, tag, description);
        out = uav_fcn(s, 'in view', ['sqrt((u(1) - ' uav_num(p.fixed_node_east_m) ')^2 + u(2)^2) < ' uav_num(p.fixed_node_radius_m)], ...
            {'tgt_e/1', 'tgt_n/1'});
        add_line(s, out, 'fixed_det/1', 'autorouting', 'on');
    case '1.4.2' % Wearable assist device: the request passes through.
        s = uav_subsystem(parent, block, {'assist_req'}, {'assist'}, tag, description);
        uav_add(s, 'Gain', 'request', 'Gain', '1');
        add_line(s, 'assist_req/1', 'request/1', 'autorouting', 'on');
        add_line(s, 'request/1', 'assist/1', 'autorouting', 'on');
    otherwise
        error('uav:wbs:unknown', 'No behavior is defined for WBS element %s.', code);
end
end

function wire(sys, code)
% Connections between the level-3 children of one level-2 element.
switch code
    case '1.1'
        af = 'WBS 1.1.1 Airframe'; pr = 'WBS 1.1.2 Propulsion'; ep = 'WBS 1.1.3 Electrical Power';
        fc = 'WBS 1.1.4 Flight Control'; co = 'WBS 1.1.5 Communications'; ob = 'WBS 1.1.6 Onboard Processing';
        lines = { ...
            [ep '/2'], [fc '/1']; [fc '/1'], [af '/1']; [fc '/2'], [af '/2']; ...
            [af '/3'], [pr '/1']; [pr '/1'], [ep '/1']; ...
            [af '/1'], [co '/1']; [af '/2'], [co '/2']; ...
            'in_fov/1', [ob '/1']; 'tgt_e/1', [ob '/2']; 'tgt_n/1', [ob '/3']; ...
            [af '/1'], 'pos_e/1'; [af '/2'], 'pos_n/1'; [co '/1'], 'link_ok/1'; ...
            [ob '/1'], 'candidate/1'; [ep '/1'], 'soc/1'};
    case '1.2'
        cam = 'WBS 1.2.1 EO Camera'; al = 'WBS 1.2.2 Audible Alarm';
        lines = { ...
            'pos_e/1', [cam '/1']; 'pos_n/1', [cam '/2']; 'tgt_e/1', [cam '/3']; 'tgt_n/1', [cam '/4']; ...
            'candidate/1', [al '/1']; 'auth/1', [al '/2']; ...
            [cam '/1'], 'in_fov/1'; [al '/1'], 'alarm_on/1'};
    case '1.3'
        gcs = 'WBS 1.3.1 Ground Control Station'; em = 'WBS 1.3.2 Event Manager';
        ng = 'WBS 1.3.3 Notification Gateway'; es = 'WBS 1.3.4 Evidence Store';
        lines = { ...
            'auth_req/1', [gcs '/1']; 'candidate/1', [em '/1']; 'link_ok/1', [em '/2']; 'fixed_det/1', [em '/3']; ...
            [em '/1'], [ng '/1']; 'in_fov/1', [es '/1']; ...
            [gcs '/1'], 'auth/1'; [em '/1'], 'event/1'; [ng '/1'], 'notified/1'; [es '/1'], 'evidence_s/1'};
    case '1.4'
        fx = 'WBS 1.4.1 Fixed Camera Node'; we = 'WBS 1.4.2 Wearable Assist Device';
        lines = { ...
            'tgt_e/1', [fx '/1']; 'tgt_n/1', [fx '/2']; 'assist_req/1', [we '/1']; ...
            [fx '/1'], 'fixed_det/1'; [we '/1'], 'assist/1'};
end
for k = 1:size(lines, 1)
    add_line(sys, lines{k, 1}, lines{k, 2}, 'autorouting', 'on');
end
end
