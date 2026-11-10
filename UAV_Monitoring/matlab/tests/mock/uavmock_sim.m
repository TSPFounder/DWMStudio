function [t, logs] = uavmock_sim(model)
%UAVMOCK_SIM  Integrate a recorded model with fixed-step RK4 (the method ode4 uses).
%   [T, LOGS] = UAVMOCK_SIM(MODEL) flattens the recorded block diagram,
%   follows every signal through subsystem ports, and integrates it. LOGS
%   has one field per To Workspace variable. This executes the wiring the
%   build scripts produced; it is not Simulink.
global UAVMOCK
h = str2double(get_param(model, 'FixedStep'));
stop = str2double(get_param(model, 'StopTime'));
n = numel(model);
paths = {};
for k = 1:numel(UAVMOCK.order)
    path = UAVMOCK.order{k};
    if strncmp(path, [model '/'], n + 1) && ~any(strcmp(UAVMOCK.blocks(path).type, {'SubSystem', 'Inport', 'Outport'}))
        paths{end + 1} = path; %#ok<AGROW>
    end
end
index = containers.Map(paths, 1:numel(paths));
count = numel(paths);
types = cell(count, 1);
inputs = cell(count, 1);
fn = cell(count, 1);
gain = zeros(count, 1);
for k = 1:count
    path = paths{k};
    types{k} = UAVMOCK.blocks(path).type;
    slash = find(path == '/', 1, 'last');
    sys = path(1:slash - 1);
    name = path(slash + 1:end);
    nin = uavmock_ports(path);
    src = zeros(1, nin);
    for port = 1:nin
        src(port) = index(resolve(sys, driver(sys, sprintf('%s/%d', name, port))));
    end
    inputs{k} = src;
    switch types{k}
        case 'Fcn'
            fn{k} = str2func(['@(u) ' uavmock_param(path, 'Expr')]);
        case 'Gain'
            gain(k) = str2double(uavmock_param(path, 'Gain'));
        case 'Constant'
            gain(k) = str2double(uavmock_param(path, 'Value'));
        case 'TransportDelay'
            gain(k) = str2double(uavmock_param(path, 'DelayTime'));
    end
end
isState = strcmp(types, 'Integrator');
isDelay = strcmp(types, 'TransportDelay');
% Evaluation order: a block after everything it reads, except through a state or a delay.
order = zeros(1, 0);
mark = zeros(count, 1);
for k = 1:count
    visit(k);
end
states = find(isState)';
delays = find(isDelay)';
x = zeros(count, 1);
for k = states
    x(k) = str2double(uavmock_param(paths{k}, 'InitialCondition'));
end
steps = round(stop / h);
t = (0:steps)' * h;
history = zeros(steps + 1, count);
sinks = find(strcmp(types, 'ToWorkspace'))';
logged = zeros(steps + 1, numel(sinks));
vals = cell(count, 1);
for step = 0:steps
    tn = step * h;
    k1 = evaluate(tn, x, step);
    for d = delays
        history(step + 1, d) = vals{inputs{d}(1)};
    end
    for s = 1:numel(sinks)
        logged(step + 1, s) = vals{inputs{sinks(s)}(1)};
    end
    if step == steps
        break;
    end
    k2 = evaluate(tn + h / 2, x + h / 2 * k1, step);
    k3 = evaluate(tn + h / 2, x + h / 2 * k2, step);
    k4 = evaluate(tn + h, x + h * k3, step);
    x = x + h / 6 * (k1 + 2 * k2 + 2 * k3 + k4);
end
logs = struct();
for s = 1:numel(sinks)
    logs.(uavmock_param(paths{sinks(s)}, 'VariableName')) = logged(:, s);
end

    function dx = evaluate(time, state, stored)
        dx = zeros(count, 1);
        for b = order
            switch types{b}
                case 'Integrator'
                    vals{b} = state(b);
                case 'Clock'
                    vals{b} = time;
                case 'Constant'
                    vals{b} = gain(b);
                case 'TransportDelay'
                    back = time - gain(b);
                    if back < 0
                        vals{b} = str2double(uavmock_param(paths{b}, 'InitialOutput'));
                    else
                        vals{b} = history(min(floor(back / h + 1e-9), stored) + 1, b);
                    end
                case 'Gain'
                    vals{b} = gain(b) * vals{inputs{b}(1)};
                case 'Mux'
                    vals{b} = [vals{inputs{b}}];
                case 'Fcn'
                    vals{b} = double(fn{b}(vals{inputs{b}(1)}));
            end
        end
        for b = states
            dx(b) = vals{inputs{b}(1)};
        end
    end

    function visit(b)
        if mark(b) == 2
            return;
        end
        if mark(b) == 1
            error('mock:sim', 'Algebraic loop through %s.', paths{b});
        end
        mark(b) = 1;
        if ~isState(b) && ~isDelay(b)
            for source = inputs{b}
                visit(source);
            end
        end
        mark(b) = 2;
        order(end + 1) = b;
    end
end

function src = driver(sys, dst)
global UAVMOCK
hit = strcmp(UAVMOCK.lines(:, 1), sys) & strcmp(UAVMOCK.lines(:, 3), dst);
if nnz(hit) ~= 1
    error('mock:sim', 'Input %s of %s has %d drivers.', dst, sys, nnz(hit));
end
src = UAVMOCK.lines{hit, 2};
end

function path = resolve(sys, src)
% Follow a signal back through subsystem boundaries to the block that computes it.
global UAVMOCK
slash = find(src == '/', 1, 'last');
name = src(1:slash - 1);
port = str2double(src(slash + 1:end));
path = [sys '/' name];
switch UAVMOCK.blocks(path).type
    case 'SubSystem'
        outs = uavmock_children(path, 'Outport');
        out = outs{port};
        path = resolve(path, driver(path, [out(numel(path) + 2:end) '/1']));
    case 'Inport'
        ins = uavmock_children(sys, 'Inport');
        number = find(strcmp(ins, path));
        slash = find(sys == '/', 1, 'last');
        path = resolve(sys(1:slash - 1), driver(sys(1:slash - 1), sprintf('%s/%d', sys(slash + 1:end), number)));
end
end
