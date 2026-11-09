function uavmock_reset()
%UAVMOCK_RESET  Empty the stand-in Simulink store used by the Octave tests.
global UAVMOCK
UAVMOCK = struct('blocks', containers.Map(), 'order', {{}}, 'lines', {cell(0, 3)}, ...
    'diagrams', containers.Map(), 'links', {cell(0, 2)});
end
