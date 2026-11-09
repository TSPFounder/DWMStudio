function e = uav_t01_expected()
%UAV_T01_EXPECTED  Known answers for the T01 mission model, from its parameters.
%   Each value follows from uav_params by hand calculation, so the model,
%   the solver and the wiring are checked against something independent.
%   These are properties of the provisional self-test scenario, not
%   requirements.

p = uav_params();
% The animal starts inside and walks east at constant speed after departing.
e.t_cross = p.cattle_depart_time_s + (p.enclosure_half_width_m - p.cattle_start_east_m) / p.cattle_speed_mps;
% The fixed node sees it between (node east - radius) and (node east + radius).
e.t_fixed_first = p.cattle_depart_time_s + (p.fixed_node_east_m - p.fixed_node_radius_m - p.cattle_start_east_m) / p.cattle_speed_mps;
e.t_fixed_last = p.cattle_depart_time_s + (p.fixed_node_east_m + p.fixed_node_radius_m - p.cattle_start_east_m) / p.cattle_speed_mps;
% The UAV flies the patrol circle at constant speed; the reserve is never reached.
power = p.hover_power_w + p.drag_power_coeff_w_per_mps3 * p.patrol_speed_mps ^ 3;
e.soc_end = 1 - power * p.stop_time_s / 3600 / p.usable_energy_wh;
e.period_s = 2 * pi * p.patrol_radius_m / p.patrol_speed_mps;
angle = p.patrol_speed_mps / p.patrol_radius_m * p.stop_time_s;
e.pos_e_end = p.patrol_radius_m * cos(angle);
e.pos_n_end = p.patrol_radius_m * sin(angle);
% The UAV first has the animal in its footprint, outside the enclosure, when
% |R*[cos(w t), sin(w t)] - [x(t), 0]| = footprint. Solved by bisection on
% the first approach (the aircraft returns to the east side after one period).
w = p.patrol_speed_mps / p.patrol_radius_m;
gap = @(t) hypot(p.patrol_radius_m * cos(w * t) - (p.cattle_start_east_m + p.cattle_speed_mps * (t - p.cattle_depart_time_s)), ...
    p.patrol_radius_m * sin(w * t)) - p.camera_footprint_radius_m;
lo = e.t_cross;
hi = e.period_s;
if gap(lo) <= 0 || gap(hi) >= 0
    error('uav:expected:bracket', 'The first UAV sighting is not bracketed; the parameters changed.');
end
for k = 1:60
    mid = (lo + hi) / 2;
    if gap(mid) > 0
        lo = mid;
    else
        hi = mid;
    end
end
e.t_uav_first = (lo + hi) / 2;
% The event opens at the earlier of the two detections; the fixed node is first here.
e.t_event = min(e.t_fixed_first, e.t_uav_first);
e.t_notified = e.t_event + p.notify_delay_s;
e.latency_s = e.t_notified - e.t_cross;
% Timing tolerance: detection is sampled on the fixed step and the event
% latch adds one step.
e.time_tolerance_s = 3 * p.fixed_step_s;
end
