function result = uav_r2025b_import_and_test(r2011aDir, outDir)
%UAV_R2025B_IMPORT_AND_TEST  Pull the R2011a WBS subsystems into an R2025b model and compare.
%   RESULT = UAV_R2025B_IMPORT_AND_TEST(R2011ADIR, OUTDIR)
%   R2011ADIR holds uav_wbs_lib.mdl and uav_t01_r2011a.mat written by
%   uav_r2011a_build_and_test in R2011a. OUTDIR must be a different folder.
%
%   The R2011a library is opened read-only from R2011ADIR and never saved
%   from this release. Its four level-2 WBS subsystems are copied, with the
%   library link broken, into a new model uav_t01_mission_r2025b.slx. A copy
%   of the whole library is also saved as uav_wbs_lib_r2025b.slx for reuse.
%
%   The new model is simulated and checked against the same hand-calculated
%   answers, then compared signal by signal with the R2011a run.
%   Prints UAV_T01_BACK_TO_BACK_PASS on success; raises an error otherwise.

if strcmpi(fullfile(r2011aDir), fullfile(outDir))
    error('uav:import:samefolder', 'Choose an output folder other than the R2011a folder, so the .mdl files stay untouched.');
end
if ~exist(outDir, 'dir')
    mkdir(outDir);
end
source = fullfile(r2011aDir, 'uav_wbs_lib.mdl');
load_system(source);
model = 'uav_t01_mission_r2025b';
file = uav_build_t01_mission(model, source, outDir, true, '.slx');

% A separate, renamed copy of the library in the current format.
copyName = 'uav_wbs_lib_r2025b';
if bdIsLoaded(copyName)
    close_system(copyName, 0);
end
set_param('uav_wbs_lib', 'Lock', 'off');
save_system('uav_wbs_lib', fullfile(outDir, [copyName '.slx']));
close_system(copyName, 0);

result = uav_t01_run(model);
result.release = version('-release');
result.model = file;
for k = 1:numel(result.lines)
    fprintf('%s\n', result.lines{k});
end
if ~result.pass
    error('uav:import:selftest', 'The imported model fails the T01 self-test in R%s.', result.release);
end

old = load(fullfile(r2011aDir, 'uav_t01_r2011a.mat'));
names = fieldnames(old.signals);
result.maxDifference = struct();
worst = 0;
if numel(old.t) ~= numel(result.t) || max(abs(old.t - result.t)) > 1e-9
    error('uav:import:time', 'The two releases did not produce the same time vector.');
end
for k = 1:numel(names)
    d = max(abs(old.signals.(names{k}) - result.signals.(names{k})));
    result.maxDifference.(names{k}) = d;
    fprintf('back-to-back  %-12s max |R2011a - R%s| = %.3g\n', names{k}, result.release, d);
    % Flags may switch one step apart; continuous signals must agree closely.
    if ~any(strcmp(names{k}, {'candidate', 'in_fov', 'event', 'notified', 'fixed_det', 'link_ok', 'alarm_on'}))
        worst = max(worst, d);
    end
end
times = {'t_cross', 't_fixed_first', 't_uav_first', 't_event', 't_notified'};
for k = 1:numel(times)
    d = abs(old.measured.(times{k}) - result.measured.(times{k}));
    fprintf('back-to-back  %-14s |difference| = %.3g s\n', times{k}, d);
    if d > result.expected.time_tolerance_s
        error('uav:import:timing', '%s differs by %.3g s between releases.', times{k}, d);
    end
end
if worst > 1e-6
    error('uav:import:signals', 'Continuous signals differ by %.3g between releases.', worst);
end
fprintf('UAV_T01_BACK_TO_BACK_PASS release=%s worst_continuous_difference=%.3g\n', result.release, worst);
end
