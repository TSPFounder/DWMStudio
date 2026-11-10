function close_system(name, varargin)
global UAVMOCK
if ~isKey(UAVMOCK.diagrams, name)
    return;
end
remove(UAVMOCK.diagrams, name);
keep = true(size(UAVMOCK.order));
for k = 1:numel(UAVMOCK.order)
    if strncmp(UAVMOCK.order{k}, [name '/'], numel(name) + 1)
        remove(UAVMOCK.blocks, UAVMOCK.order{k});
        keep(k) = false;
    end
end
UAVMOCK.order = UAVMOCK.order(keep);
if ~isempty(UAVMOCK.lines)
    sys = UAVMOCK.lines(:, 1);
    gone = strcmp(sys, name) | strncmp(sys, [name '/'], numel(name) + 1);
    UAVMOCK.lines = UAVMOCK.lines(~gone, :);
end
end
