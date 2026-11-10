function add_block(src, dst, varargin)
%ADD_BLOCK  Stand-in: records the block and rejects what Simulink would reject.
global UAVMOCK
if mod(numel(varargin), 2) ~= 0
    error('mock:add_block', 'Parameters must be name/value pairs.');
end
slash = find(dst == '/', 1, 'last');
parent = dst(1:slash - 1);
if ~isKey(UAVMOCK.diagrams, parent)
    if ~isKey(UAVMOCK.blocks, parent) || ~strcmp(UAVMOCK.blocks(parent).type, 'SubSystem')
        error('mock:add_block', 'Parent %s does not exist or is not a subsystem.', parent);
    end
end
if isKey(UAVMOCK.blocks, dst)
    error('mock:add_block', 'Block %s already exists.', dst);
end
if strncmp(src, 'built-in/', 9)
    type = src(10:end);
    if strcmpi(type, 'Subsystem')
        type = 'SubSystem';
    end
    known = {'SubSystem', 'Inport', 'Outport', 'Fcn', 'Mux', 'Integrator', 'Gain', 'TransportDelay', ...
        'Clock', 'Constant', 'ToWorkspace'};
    if ~any(strcmp(type, known))
        error('mock:add_block', 'Block type %s is not one the mock knows.', type);
    end
    UAVMOCK.blocks(dst) = struct('type', type, 'params', {varargin});
    UAVMOCK.order{end + 1} = dst;
else
    if ~isKey(UAVMOCK.blocks, src)
        error('mock:add_block', 'Source block %s does not exist.', src);
    end
    n = numel(src);
    order = UAVMOCK.order;
    for k = 1:numel(order)
        path = order{k};
        if strcmp(path, src) || strncmp(path, [src '/'], n + 1)
            copy = UAVMOCK.blocks(path);
            if strcmp(path, src)
                copy.params = [copy.params, varargin];
            end
            UAVMOCK.blocks([dst path(n + 1:end)]) = copy;
            UAVMOCK.order{end + 1} = [dst path(n + 1:end)];
        end
    end
    lines = UAVMOCK.lines;
    for k = 1:size(lines, 1)
        sys = lines{k, 1};
        if strcmp(sys, src) || strncmp(sys, [src '/'], n + 1)
            UAVMOCK.lines(end + 1, :) = {[dst sys(n + 1:end)], lines{k, 2}, lines{k, 3}};
        end
    end
end
end
