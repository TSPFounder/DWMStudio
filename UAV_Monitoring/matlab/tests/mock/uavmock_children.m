function list = uavmock_children(sys, type)
%UAVMOCK_CHILDREN  Direct children of SYS, optionally of one type, in creation order.
global UAVMOCK
list = {};
n = numel(sys);
for k = 1:numel(UAVMOCK.order)
    path = UAVMOCK.order{k};
    if strncmp(path, [sys '/'], n + 1) && ~any(path(n + 2:end) == '/')
        if nargin < 2 || strcmp(UAVMOCK.blocks(path).type, type)
            list{end + 1} = path; %#ok<AGROW>
        end
    end
end
end
