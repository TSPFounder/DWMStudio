function result = uav_r2011a_build_and_test(packageRoot, outDir)
%UAV_R2011A_BUILD_AND_TEST  Build the WBS library and T01 model in R2011a and self-test.
%   RESULT = UAV_R2011A_BUILD_AND_TEST(PACKAGEROOT, OUTDIR)
%   PACKAGEROOT is the UAV_Monitoring folder. OUTDIR receives
%   uav_wbs_lib.mdl, uav_t01_mission.mdl, uav_t01_r2011a.mat and
%   uav_t01_r2011a_selftest.txt.
%
%   Prints UAV_T01_SELFTEST_PASS release=<release> on success and raises an
%   error otherwise. A model that builds but fails a check is a failure.
%
%   Example (R2011a):
%     addpath('F:\DWMStudio\UAV_Monitoring\matlab\common', 'F:\DWMStudio\UAV_Monitoring\matlab\r2011a')
%     uav_r2011a_build_and_test('F:\DWMStudio\UAV_Monitoring', 'F:\UAV_Models')

lib = uav_build_wbs_library(outDir, fullfile(packageRoot, 'baseline', 'wbs_elements.tsv'));
addpath(outDir);
model = 'uav_t01_mission';
file = uav_build_t01_mission(model, lib, outDir, false, '.mdl');
result = uav_t01_run(model);
result.release = version('-release');
result.library = lib;
result.model = file;

report = fullfile(outDir, ['uav_t01_r' result.release '_selftest.txt']);
fid = fopen(report, 'w');
fprintf(fid, 'T01 mission model self-test, MATLAB R%s, %s\n', result.release, datestr(now, 31));
fprintf(fid, 'Simulation evidence only. Parameters are provisional, not approved thresholds.\n');
for k = 1:numel(result.lines)
    fprintf(fid, '%s\n', result.lines{k});
    fprintf('%s\n', result.lines{k});
end
fclose(fid);
t = result.t; %#ok<NASGU>
signals = result.signals; %#ok<NASGU>
measured = result.measured; %#ok<NASGU>
save(fullfile(outDir, ['uav_t01_r' result.release '.mat']), '-v7', 't', 'signals', 'measured');
if ~result.pass
    error('uav:selftest:fail', 'T01 self-test failed in R%s. See %s.', result.release, report);
end
fprintf('UAV_T01_SELFTEST_PASS release=%s\n', result.release);
end
