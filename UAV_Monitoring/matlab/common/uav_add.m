function path = uav_add(sys, type, name, varargin)
%UAV_ADD  Add a built-in block to SYS, placed on a grid, and return its path.
%   PATH = UAV_ADD(SYS, TYPE, NAME, 'Param', 'Value', ...) adds
%   built-in/TYPE as SYS/NAME. Blocks are laid out six to a row in the order
%   they are added so a generated diagram is readable without hand layout.

path = [sys '/' name];
existing = find_system(sys, 'SearchDepth', 1, 'Type', 'block');
n = numel(existing);
if n > 0 && strcmp(get_param(sys, 'Type'), 'block')
    n = n - 1; % find_system lists the subsystem itself
end
left = 40 + 150 * mod(n, 6);
top = 40 + 100 * floor(n / 6);
position = [left top left + 90 top + 50];
if strcmp(type, 'SubSystem')
    % The built-in name has been spelled both ways across releases.
    try
        add_block('built-in/Subsystem', path, 'Position', position, varargin{:});
    catch %#ok<CTCH>
        add_block('built-in/SubSystem', path, 'Position', position, varargin{:});
    end
else
    add_block(['built-in/' type], path, 'Position', position, varargin{:});
end
end
