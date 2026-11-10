function add_line(sys, src, dst, varargin)
%ADD_LINE  Stand-in: checks both ends exist, ports are in range, and the input is free.
global UAVMOCK
[sb, sp] = splitPort(src);
[db, dp] = splitPort(dst);
sPath = [sys '/' sb];
dPath = [sys '/' db];
if ~isKey(UAVMOCK.blocks, sPath)
    error('mock:add_line', 'In %s: source block "%s" does not exist.', sys, sb);
end
if ~isKey(UAVMOCK.blocks, dPath)
    error('mock:add_line', 'In %s: destination block "%s" does not exist.', sys, db);
end
[~, nout] = uavmock_ports(sPath);
[nin, ~] = uavmock_ports(dPath);
if sp < 1 || sp > nout
    error('mock:add_line', 'In %s: "%s" has %d output ports; port %d was asked for.', sys, sb, nout, sp);
end
if dp < 1 || dp > nin
    error('mock:add_line', 'In %s: "%s" has %d input ports; port %d was asked for.', sys, db, nin, dp);
end
for k = 1:size(UAVMOCK.lines, 1)
    if strcmp(UAVMOCK.lines{k, 1}, sys) && strcmp(UAVMOCK.lines{k, 3}, dst)
        error('mock:add_line', 'In %s: input %s is already connected.', sys, dst);
    end
end
UAVMOCK.lines(end + 1, :) = {sys, src, dst};
end

function [block, port] = splitPort(text)
slash = find(text == '/', 1, 'last');
block = text(1:slash - 1);
port = str2double(text(slash + 1:end));
end
