function uav_mock_tests()
%UAV_MOCK_TESTS  Run the Simulink build scripts against the Octave stand-in.
%   octave --no-gui --eval "cd matlab/tests; uav_mock_tests"
%
%   Checks, without MATLAB: the WBS library builds one subsystem per WBS row;
%   every connection names blocks and ports that exist; every input is
%   driven; the mission model assembled from the library integrates to the
%   hand-calculated answers; the parameters match the baseline JSON.
%   What this cannot check: that R2011a accepts each block parameter name.

here = fileparts(mfilename('fullpath'));
root = fileparts(fileparts(here));
addpath(fullfile(here, 'mock'), fullfile(root, 'matlab', 'common'), fullfile(root, 'matlab', 'r2011a'));
out = tempname();
uavmock_reset();
global UAVMOCK

wbsFile = fullfile(root, 'baseline', 'wbs_elements.tsv');
lib = uav_build_wbs_library(out, wbsFile);
wbs = uav_read_tsv(wbsFile);
subs = find_system('uav_wbs_lib', 'BlockType', 'SubSystem');
assert(numel(subs) == numel(wbs.Code), 'one subsystem per WBS row');
for k = 1:numel(wbs.Code)
    path = ['uav_wbs_lib/' wbs.BlockPath{k}];
    assert(isKey(UAVMOCK.blocks, path), ['missing ' path]);
    assert(strcmp(get_param(path, 'Tag'), ['WBS ' wbs.Code{k}]), ['tag of ' path]);
end
printf('PASS  library: %d WBS subsystems, tagged, all internal inputs driven\n', numel(subs));

alloc = uav_read_tsv(fullfile(root, 'baseline', 'requirement_allocation.tsv'));
for k = 1:numel(alloc.BlockPath)
    assert(isKey(UAVMOCK.blocks, ['uav_wbs_lib/' alloc.BlockPath{k}]), ['allocation names missing block ' alloc.BlockPath{k}]);
end
printf('PASS  allocation: %d requirement-to-block rows all name library blocks\n', numel(alloc.BlockPath));

text = fileread(fullfile(root, 'baseline', 'uav_baseline.json'));
p = uav_params();
names = fieldnames(p);
for k = 1:numel(names)
    tok = regexp(text, ['"' names{k} '":\s*([-0-9.eE]+)'], 'tokens', 'once');
    assert(~isempty(tok), ['baseline lacks ' names{k}]);
    assert(str2double(tok{1}) == p.(names{k}), ['parameter ' names{k} ' differs from the baseline']);
end
printf('PASS  parameters: %d values equal the baseline JSON\n', numel(names));

for breakLinks = [false true]
    uav_build_t01_mission('uav_t01_mission', lib, out, breakLinks, '.mdl');
    [t, logs] = uavmock_sim('uav_t01_mission');
    s = struct();
    fields = fieldnames(logs);
    for k = 1:numel(fields)
        s.(fields{k}(5:end)) = logs.(fields{k});
    end
    result = uav_t01_check(t, s);
    for k = 1:numel(result.lines)
        printf('%s\n', result.lines{k});
    end
    assert(result.pass, 'mission model does not reproduce the hand-calculated answers');
end
m = result.measured;
printf('PASS  mission model (linked and unlinked): crossing %.2f s, event %.2f s, notified %.2f s, UAV sighting %.2f s\n', ...
    m.t_cross, m.t_event, m.t_notified, m.t_uav_first);

% A wrong connection must be caught, or the checks above prove nothing.
caught = false;
try
    add_line('uav_t01_mission', 'Scenario/1', 'WBS 1.1 Air Vehicle/1', 'autorouting', 'on');
catch
    caught = true;
end
assert(caught, 'a doubly driven input was accepted');
caught = false;
try
    add_line('uav_t01_mission', 'Scenario/9', 'log soc/1', 'autorouting', 'on');
catch
    caught = true;
end
assert(caught, 'a port that does not exist was accepted');
printf('PASS  the stand-in rejects a doubly driven input and a missing port\n');
printf('UAV_MOCK_TESTS_PASS\n');
end
