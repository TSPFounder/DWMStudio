function [nin, nout] = uavmock_ports(path)
%UAVMOCK_PORTS  Number of input and output ports of a recorded block.
global UAVMOCK
b = UAVMOCK.blocks(path);
switch b.type
    case 'SubSystem'
        nin = numel(uavmock_children(path, 'Inport'));
        nout = numel(uavmock_children(path, 'Outport'));
    case 'Inport'
        nin = 0; nout = 1;
    case {'Outport', 'ToWorkspace'}
        nin = 1; nout = 0;
    case {'Clock', 'Constant'}
        nin = 0; nout = 1;
    case 'Mux'
        nin = str2double(uavmock_param(path, 'Inputs')); nout = 1;
    otherwise
        nin = 1; nout = 1;
end
end
