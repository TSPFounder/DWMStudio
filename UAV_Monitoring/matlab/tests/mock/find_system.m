function list = find_system(sys, varargin)
%FIND_SYSTEM  Stand-in for the two forms the build scripts use:
%   find_system(SYS, 'SearchDepth', 1, 'Type', 'block')  SYS itself (if a block) and its children
%   find_system(SYS, 'BlockType', 'SubSystem')            every subsystem under SYS
global UAVMOCK
if numel(varargin) == 4 && strcmp(varargin{1}, 'SearchDepth')
    list = uavmock_children(sys);
    if isKey(UAVMOCK.blocks, sys)
        list = [{sys}, list];
    end
elseif numel(varargin) == 2 && strcmp(varargin{1}, 'BlockType')
    list = {};
    n = numel(sys);
    for k = 1:numel(UAVMOCK.order)
        path = UAVMOCK.order{k};
        if strncmp(path, [sys '/'], n + 1) && strcmp(UAVMOCK.blocks(path).type, varargin{2})
            list{end + 1} = path; %#ok<AGROW>
        end
    end
else
    error('mock:find_system', 'This form of find_system is not in the mock.');
end
list = list(:);
end
