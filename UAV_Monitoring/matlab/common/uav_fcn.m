function out = uav_fcn(sys, name, expr, sources)
%UAV_FCN  Add a Fcn block fed by one or more signals; return its output port.
%   OUT = UAV_FCN(SYS, NAME, EXPR, SOURCES) adds Fcn block NAME with
%   expression EXPR. SOURCES is a cell array of 'Block/port' strings. With
%   one source the expression uses u; with several a Mux is added and the
%   expression uses u(1), u(2), ... in the order given.

if numel(sources) > 1
    uav_add(sys, 'Mux', [name ' mux'], 'Inputs', sprintf('%d', numel(sources)));
    for k = 1:numel(sources)
        add_line(sys, sources{k}, sprintf('%s mux/%d', name, k), 'autorouting', 'on');
    end
    uav_add(sys, 'Fcn', name, 'Expr', expr);
    add_line(sys, [name ' mux/1'], [name '/1'], 'autorouting', 'on');
else
    uav_add(sys, 'Fcn', name, 'Expr', expr);
    add_line(sys, sources{1}, [name '/1'], 'autorouting', 'on');
end
out = [name '/1'];
end
