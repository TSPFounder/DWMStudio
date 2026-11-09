function result = uav_t01_check(t, s)
%UAV_T01_CHECK  Compare a T01 run with the hand-calculated answers.
%   RESULT = UAV_T01_CHECK(T, S). T is the time vector. S is a struct of
%   logged arrays with fields tgt_e, pos_e, pos_n, link_ok, candidate, soc,
%   in_fov, alarm_on, event, notified, fixed_det.
%   RESULT.pass is true only when every check passes; RESULT.lines holds
%   one line of text per check, and RESULT.measured the measured values.

e = uav_t01_expected();
m.t_cross = firstTime(t, abs(s.tgt_e) > uavHalfWidth());
m.t_fixed_first = firstTime(t, s.fixed_det > 0.5);
m.t_uav_first = firstTime(t, s.candidate > 0.5);
m.t_event = firstTime(t, s.event > 0.5);
m.t_notified = firstTime(t, s.notified > 0.5);
m.latency_s = m.t_notified - m.t_cross;
m.soc_end = s.soc(end);
m.pos_e_end = s.pos_e(end);
m.pos_n_end = s.pos_n(end);
m.alarm_max = max(s.alarm_on);
m.link_min = min(s.link_ok);

tol = e.time_tolerance_s;
checks = { ...
    'ground-truth crossing time [s]', m.t_cross, e.t_cross, tol; ...
    'fixed node first detection [s]', m.t_fixed_first, e.t_fixed_first, tol; ...
    'UAV first candidate [s]', m.t_uav_first, e.t_uav_first, tol; ...
    'event opened [s]', m.t_event, e.t_event, tol; ...
    'recipient notified [s]', m.t_notified, e.t_notified, tol + 0.05; ...
    'notification latency [s]', m.latency_s, e.latency_s, 2 * tol + 0.05; ...
    'state of charge at end [-]', m.soc_end, e.soc_end, 1e-6; ...
    'east position at end [m]', m.pos_e_end, e.pos_e_end, 1e-3; ...
    'north position at end [m]', m.pos_n_end, e.pos_n_end, 1e-3; ...
    'alarm stays off without authorization [-]', m.alarm_max, 0, 0; ...
    'link available throughout [-]', m.link_min, 1, 0};
result.pass = true;
result.lines = cell(size(checks, 1), 1);
for k = 1:size(checks, 1)
    ok = ~isnan(checks{k, 2}) && abs(checks{k, 2} - checks{k, 3}) <= checks{k, 4};
    result.pass = result.pass && ok;
    if ok
        word = 'PASS';
    else
        word = 'FAIL';
    end
    result.lines{k} = sprintf('%s  %-44s measured %.6f  expected %.6f  tolerance %.3g', ...
        word, checks{k, 1}, checks{k, 2}, checks{k, 3}, checks{k, 4});
end
result.measured = m;
result.expected = e;
end

function w = uavHalfWidth()
p = uav_params();
w = p.enclosure_half_width_m;
end

function value = firstTime(t, condition)
k = find(condition, 1);
if isempty(k)
    value = NaN;
else
    value = t(k);
end
end
