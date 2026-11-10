function p = uav_params()
%UAV_PARAMS  Parameters of the T01 mission model.
%   Provisional engineering values chosen for a deterministic self-test.
%   They are NOT approved thresholds and NOT a sized aircraft. They mirror
%   model_parameters in baseline/uav_baseline.json; uav_mock_tests checks
%   that the two agree.

p.patrol_radius_m = 150;
p.patrol_speed_mps = 12;
p.enclosure_half_width_m = 100;
p.camera_footprint_radius_m = 60;
p.link_range_m = 1000;
p.notify_delay_s = 5;
p.cattle_start_east_m = 80;
p.cattle_speed_mps = 1;
p.cattle_depart_time_s = 20;
p.hover_power_w = 180;
p.drag_power_coeff_w_per_mps3 = 0.05;
p.usable_energy_wh = 90;
p.reserve_fraction = 0.2;
p.fixed_node_east_m = 130;
p.fixed_node_radius_m = 25;
p.stop_time_s = 300;
p.fixed_step_s = 0.05;
end
