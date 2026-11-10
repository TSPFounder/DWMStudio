function result = uav_build_power_simscape(outDir)
%UAV_BUILD_POWER_SIMSCAPE  Build and check a Simscape power and propulsion model (R2025b).
%   RESULT = UAV_BUILD_POWER_SIMSCAPE(OUTDIR) creates uav_power_simscape.slx
%   with two physical subsystems that match WBS elements:
%
%     ElectricalPower (WBS 1.1.3)  DC source with internal resistance
%     Propulsion      (WBS 1.1.2)  winding resistance, rotational
%                                  electromechanical converter, rotor
%                                  inertia and a viscous propeller load
%
%   Only Simscape Foundation blocks are used. Values are read back from
%   Simscape logging, so no sensor blocks are needed.
%
%   Check: the steady state of this network has a closed form,
%       w = K*V / (K^2 + (Rb + Rw)*D)      i = D*w / K
%   and the run must match both within 1 %. The converter's port order is
%   detected, not assumed, so w may come out negative; the check uses
%   magnitudes and reports the sign.
%
%   R2011a has no Simscape on the licence recorded in PD Quick's TOOLING.md,
%   so this model starts in R2025b. NOT a propeller or battery model: the
%   load is linear and the source is ideal. Parameters are provisional.

q.V = 14.8;      % V    source voltage (4S pack nominal)
q.Rb = 0.03;     % Ohm  source internal resistance
q.Rw = 0.12;     % Ohm  motor winding resistance
q.K = 0.012;     % V*s/rad and N*m/A
q.J = 4e-5;      % kg*m^2 rotor and propeller inertia
q.D = 3e-5;      % N*m*s/rad linearized propeller load

model = 'uav_power_simscape';
if bdIsLoaded(model)
    close_system(model, 0);
end
new_system(model);
load_system('fl_lib');
load_system('nesl_utility');

ep = [model '/ElectricalPower'];
add_block('built-in/Subsystem', ep, 'Position', [80 80 220 180]);
set_param(ep, 'Tag', 'WBS 1.1.3', 'AttributesFormatString', '%<Tag>');
foundation(ep, 'DC Voltage Source', 'Source', {'v0', num2str(q.V)});
foundation(ep, 'Resistor', 'InternalResistance', {'R', num2str(q.Rb)});
foundation(ep, 'Electrical Reference', 'Ground', {});
connectionPort(ep, 'pos', 'right');
connectionPort(ep, 'neg', 'right');
physical(ep, 'Source', 'LConn1', 'InternalResistance', 'LConn1');
physical(ep, 'InternalResistance', 'RConn1', 'pos', 'Conn1');
physical(ep, 'Source', 'RConn1', 'neg', 'Conn1');
physical(ep, 'Source', 'RConn1', 'Ground', 'Conn1');

pr = [model '/Propulsion'];
add_block('built-in/Subsystem', pr, 'Position', [320 80 460 180]);
set_param(pr, 'Tag', 'WBS 1.1.2', 'AttributesFormatString', '%<Tag>');
foundation(pr, 'Resistor', 'Winding', {'R', num2str(q.Rw)});
foundation(pr, 'Rotational Electromechanical Converter', 'Motor', {'K', num2str(q.K)});
foundation(pr, 'Inertia', 'Rotor', {'inertia', num2str(q.J)});
foundation(pr, 'Rotational Damper', 'PropellerLoad', {'D', num2str(q.D)});
foundation(pr, 'Mechanical Rotational Reference', 'Case', {});
connectionPort(pr, 'pos', 'left');
connectionPort(pr, 'neg', 'left');
% The converter has two electrical and two mechanical ports, and which is
% which differs from what its icon suggests between releases. Each port is
% tested against a known electrical port, so nothing is assumed.
[electrical, mechanical] = classifyPorts(pr, 'Motor', 'Winding', 'RConn1');
physical(pr, 'pos', 'Conn1', 'Winding', 'LConn1');
physical(pr, 'Winding', 'RConn1', 'Motor', electrical(1));
physical(pr, 'Motor', electrical(2), 'neg', 'Conn1');
physical(pr, 'Motor', mechanical(1), 'Rotor', 'Conn1');
physical(pr, 'Motor', mechanical(1), 'PropellerLoad', 'LConn1');
physical(pr, 'PropellerLoad', 'RConn1', 'Case', 'Conn1');
physical(pr, 'Motor', mechanical(2), 'Case', 'Conn1');

add_block('nesl_utility/Solver Configuration', [model '/Solver'], 'Position', [80 240 140 280]);
physical(model, 'ElectricalPower', 'RConn1', 'Propulsion', 'LConn1');
physical(model, 'ElectricalPower', 'RConn2', 'Propulsion', 'LConn2');
physical(model, 'Solver', 'Conn1', 'ElectricalPower', 'RConn2');

set_param(model, 'Solver', 'ode23t', 'StopTime', '5', 'SimscapeLogType', 'all', 'SimscapeLogName', 'simlog');
if ~exist(outDir, 'dir')
    mkdir(outDir);
end
result.model = fullfile(outDir, [model '.slx']);
save_system(model, result.model);

out = sim(model, 'ReturnWorkspaceOutputs', 'on');
log = out.get('simlog');
w = log.Propulsion.Rotor.w.series.values('rad/s');
i = log.Propulsion.Winding.i.series.values('A');
result.w = w(end);
result.i = i(end);
result.w_expected = q.K * q.V / (q.K ^ 2 + (q.Rb + q.Rw) * q.D);
result.i_expected = q.D * result.w_expected / q.K;
result.electrical_power_w = q.V * abs(result.i) - q.Rb * result.i ^ 2;
result.parameters = q;
okW = abs(abs(result.w) - result.w_expected) <= 0.01 * result.w_expected;
okI = abs(abs(result.i) - result.i_expected) <= 0.01 * result.i_expected;
fprintf('rotor speed     measured %.3f rad/s   expected %.3f rad/s\n', result.w, result.w_expected);
fprintf('motor current   measured %.4f A       expected %.4f A\n', result.i, result.i_expected);
if result.w < 0
    fprintf('Note: speed is negative. That is the port order the converter was connected in; magnitudes are unaffected.\n');
end
if ~(okW && okI)
    error('uav:simscape:check', 'The Simscape model does not match the closed-form steady state within 1 %%.');
end
fprintf('UAV_SIMSCAPE_PASS release=%s\n', version('-release'));
end

function foundation(sys, blockName, newName, params)
% Copy a Foundation library block found by name, wherever the release keeps it.
found = find_system('fl_lib', 'LookUnderMasks', 'all', 'FollowLinks', 'on', 'SearchDepth', 6, 'Name', blockName);
if isempty(found)
    error('uav:simscape:block', 'Foundation block "%s" was not found in fl_lib in this release.', blockName);
end
n = numel(find_system(sys, 'SearchDepth', 1, 'Type', 'block')) - 1;
left = 40 + 140 * mod(n, 4);
top = 40 + 110 * floor(n / 4);
path = [sys '/' newName];
add_block(found{1}, path, 'Position', [left top left + 60 top + 60]);
for k = 1:2:numel(params)
    try
        set_param(path, params{k}, params{k + 1});
    catch err
        names = fieldnames(get_param(path, 'DialogParameters'));
        error('uav:simscape:param', 'Block "%s" has no parameter "%s" (%s). Its parameters are: %s', ...
            blockName, params{k}, err.message, sprintf('%s ', names{:}));
    end
end
end

function connectionPort(sys, name, side)
n = numel(find_system(sys, 'SearchDepth', 1, 'Type', 'block')) - 1;
add_block('nesl_utility/Connection Port', [sys '/' name], 'Side', side, ...
    'Position', [40 + 140 * mod(n, 4), 40 + 110 * floor(n / 4), 70 + 140 * mod(n, 4), 58 + 110 * floor(n / 4)]);
end

function physical(sys, a, portA, b, portB)
% Physical connection between two named ports (LConn1, RConn2, ...). Conn1 is
% the only conserving port of a one-port block, whichever side it is drawn on.
add_line(sys, portHandle([sys '/' a], portA), portHandle([sys '/' b], portB), 'autorouting', 'on');
end

function [electrical, mechanical] = classifyPorts(sys, block, electricalBlock, electricalPort)
% Split BLOCK's conserving ports by whether Simulink allows a connection to a
% known electrical port. The trial lines are removed again.
handles = get_param([sys '/' block], 'PortHandles');
all = [handles.LConn, handles.RConn];
probe = portHandle([sys '/' electricalBlock], electricalPort);
electrical = [];
mechanical = [];
for k = 1:numel(all)
    try
        line = add_line(sys, all(k), probe);
        delete_line(line);
        electrical(end + 1) = all(k); %#ok<AGROW>
    catch %#ok<CTCH>
        mechanical(end + 1) = all(k); %#ok<AGROW>
    end
end
if numel(electrical) ~= 2 || numel(mechanical) ~= 2
    error('uav:simscape:ports', '%s has %d electrical and %d other conserving ports; two of each were expected.', ...
        block, numel(electrical), numel(mechanical));
end
end

function h = portHandle(block, port)
if isnumeric(port)
    h = port;
    return;
end
handles = get_param(block, 'PortHandles');
if strncmp(port, 'Conn', 4)
    side = 'conserving';
    index = str2double(port(5:end));
    list = [handles.LConn, handles.RConn];
else
    side = port(1:5);
    index = str2double(port(6:end));
    list = handles.(side);
end
if index > numel(list)
    error('uav:simscape:port', '%s has %d %s ports; %s was asked for.', block, numel(list), side, port);
end
h = list(index);
end
