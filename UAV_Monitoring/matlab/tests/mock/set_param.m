function set_param(path, varargin)
global UAVMOCK
if mod(numel(varargin), 2) ~= 0
    error('mock:set_param', 'Parameters must be name/value pairs.');
end
if isKey(UAVMOCK.diagrams, path)
    d = UAVMOCK.diagrams(path);
    d.params = [d.params, varargin];
    UAVMOCK.diagrams(path) = d;
elseif isKey(UAVMOCK.blocks, path)
    b = UAVMOCK.blocks(path);
    b.params = [b.params, varargin];
    UAVMOCK.blocks(path) = b;
else
    error('mock:set_param', '%s does not exist.', path);
end
end
