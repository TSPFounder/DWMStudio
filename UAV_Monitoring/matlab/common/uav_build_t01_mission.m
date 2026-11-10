function modelFile = uav_build_t01_mission(model, libFile, outDir, breakLinks, extension)
%UAV_BUILD_T01_MISSION  Build the T01 escaped-cattle mission model from the WBS library.
%   MODELFILE = UAV_BUILD_T01_MISSION(MODEL, LIBFILE, OUTDIR, BREAKLINKS, EXT)
%   creates MODEL in OUTDIR with extension EXT ('.mdl' or '.slx'). The four
%   level-2 WBS subsystems are copied from the library LIBFILE. With
%   BREAKLINKS true the copies are made independent of the library, which
%   is how a later release takes the R2011a subsystems into its own model.
%
%   A Scenario subsystem supplies ground truth: one animal walking east out
%   of the enclosure. Eleven signals are logged to the workspace as arrays.
%
%   Runs unchanged in R2011a and R2025b.

p = uav_params();
[~, lib] = fileparts(libFile);
if ~bdIsLoaded(lib)
    load_system(libFile);
end
if bdIsLoaded(model)
    close_system(model, 0);
end
new_system(model);

% Ground truth. Not a WBS element: it is the test stimulus.
sc = uav_subsystem(model, 'Scenario', {}, {'tgt_e', 'tgt_n', 'auth_req', 'assist_req'}, 'Scenario', ...
    'Ground truth: one animal leaves the enclosure eastward. No alarm authorization, no assistance request.');
uav_add(sc, 'Clock', 'time');
east = uav_fcn(sc, 'east position', [uav_num(p.cattle_start_east_m) ' + ' uav_num(p.cattle_speed_mps) ...
    '*(u > ' uav_num(p.cattle_depart_time_s) ')*(u - ' uav_num(p.cattle_depart_time_s) ')'], {'time/1'});
add_line(sc, east, 'tgt_e/1', 'autorouting', 'on');
uav_add(sc, 'Constant', 'north position', 'Value', '0');
uav_add(sc, 'Constant', 'no authorization', 'Value', '0');
uav_add(sc, 'Constant', 'no request', 'Value', '0');
add_line(sc, 'north position/1', 'tgt_n/1', 'autorouting', 'on');
add_line(sc, 'no authorization/1', 'auth_req/1', 'autorouting', 'on');
add_line(sc, 'no request/1', 'assist_req/1', 'autorouting', 'on');

names = {'WBS 1.1 Air Vehicle', 'WBS 1.2 Payload', 'WBS 1.3 Ground Segment', 'WBS 1.4 Fixed and Wearable Sensing'};
for k = 1:numel(names)
    add_block([lib '/' names{k}], [model '/' names{k}], 'Position', [60 + 260 * (k - 1), 220, 240 + 260 * (k - 1), 420]);
    if breakLinks
        set_param([model '/' names{k}], 'LinkStatus', 'none');
    end
end
av = names{1}; pl = names{2}; gs = names{3}; fw = names{4};
lines = { ...
    'Scenario/1', [av '/1']; 'Scenario/2', [av '/2']; [pl '/1'], [av '/3']; ...
    [av '/1'], [pl '/1']; [av '/2'], [pl '/2']; 'Scenario/1', [pl '/3']; 'Scenario/2', [pl '/4']; ...
    [av '/4'], [pl '/5']; [gs '/1'], [pl '/6']; ...
    'Scenario/3', [gs '/1']; [av '/4'], [gs '/2']; [av '/3'], [gs '/3']; [fw '/1'], [gs '/4']; [pl '/1'], [gs '/5']; ...
    'Scenario/1', [fw '/1']; 'Scenario/2', [fw '/2']; 'Scenario/4', [fw '/3']};
for k = 1:size(lines, 1)
    add_line(model, lines{k, 1}, lines{k, 2}, 'autorouting', 'on');
end

logs = { ...
    'tgt_e', 'Scenario/1'; 'pos_e', [av '/1']; 'pos_n', [av '/2']; 'link_ok', [av '/3']; ...
    'candidate', [av '/4']; 'soc', [av '/5']; 'in_fov', [pl '/1']; 'alarm_on', [pl '/2']; ...
    'event', [gs '/2']; 'notified', [gs '/3']; 'evidence_s', [gs '/4']; 'fixed_det', [fw '/1']; 'assist', [fw '/2']};
for k = 1:size(logs, 1)
    uav_add(model, 'ToWorkspace', ['log ' logs{k, 1}], 'VariableName', ['uav_' logs{k, 1}], ...
        'SaveFormat', 'Array', 'MaxDataPoints', 'inf');
    add_line(model, logs{k, 2}, ['log ' logs{k, 1} '/1'], 'autorouting', 'on');
end
set_param(model, 'SolverType', 'Fixed-step', 'Solver', 'ode4', 'FixedStep', uav_num(p.fixed_step_s), ...
    'StartTime', '0', 'StopTime', uav_num(p.stop_time_s), 'SaveTime', 'on', 'TimeSaveName', 'uav_t', ...
    'SaveOutput', 'off', 'LimitDataPoints', 'off');

if ~exist(outDir, 'dir')
    mkdir(outDir);
end
modelFile = fullfile(outDir, [model extension]);
save_system(model, modelFile);
end
