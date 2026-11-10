function result = uav_t01_run(model)
%UAV_T01_RUN  Simulate a built T01 mission model and check it.
%   RESULT = UAV_T01_RUN(MODEL) runs the loaded model MODEL, collects the
%   logged arrays and returns the result of UAV_T01_CHECK with the time
%   vector in RESULT.t and the signals in RESULT.signals.

out = sim(model, 'ReturnWorkspaceOutputs', 'on');
t = out.get('uav_t');
names = {'tgt_e', 'pos_e', 'pos_n', 'link_ok', 'candidate', 'soc', 'in_fov', 'alarm_on', ...
    'event', 'notified', 'evidence_s', 'fixed_det', 'assist'};
s = struct();
for k = 1:numel(names)
    s.(names{k}) = out.get(['uav_' names{k}]);
end
result = uav_t01_check(t, s);
result.t = t;
result.signals = s;
end
