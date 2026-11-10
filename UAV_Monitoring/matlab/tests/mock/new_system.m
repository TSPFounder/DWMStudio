function new_system(name, kind)
global UAVMOCK
if nargin < 2
    kind = 'Model';
end
if isKey(UAVMOCK.diagrams, name)
    error('mock:new_system', 'A block diagram named %s already exists.', name);
end
UAVMOCK.diagrams(name) = struct('kind', kind, 'file', '', 'params', {{}});
end
