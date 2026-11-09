function uavmock_check(name)
%UAVMOCK_CHECK  Every input port under diagram NAME must be driven.
global UAVMOCK
n = numel(name);
for k = 1:numel(UAVMOCK.order)
    path = UAVMOCK.order{k};
    if ~strncmp(path, [name '/'], n + 1)
        continue;
    end
    slash = find(path == '/', 1, 'last');
    sys = path(1:slash - 1);
    block = path(slash + 1:end);
    nin = uavmock_ports(path);
    % A library's top-level blocks are definitions; their own inputs are left open.
    if strcmp(sys, name) && strcmpi(UAVMOCK.diagrams(name).kind, 'Library')
        continue;
    end
    for port = 1:nin
        dst = sprintf('%s/%d', block, port);
        hit = strcmp(UAVMOCK.lines(:, 1), sys) & strcmp(UAVMOCK.lines(:, 3), dst);
        if ~any(hit)
            error('mock:check', 'Input %s of %s is not connected.', dst, sys);
        end
    end
end
end
