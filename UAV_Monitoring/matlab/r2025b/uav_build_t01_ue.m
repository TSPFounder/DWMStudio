function result = uav_build_t01_ue(r2025bDir, outDir, runIt)
%UAV_BUILD_T01_UE  Add an Unreal Engine scene to the T01 model and run it (R2025b).
%   RESULT = UAV_BUILD_T01_UE(R2025BDIR, OUTDIR, RUNIT)
%   R2025BDIR holds uav_t01_mission_r2025b.slx from uav_r2025b_import_and_test.
%   Creates uav_t01_ue.slx in OUTDIR: the same mission model with the
%   aircraft drawn in the Unreal Engine viewer that Simulink 3D co-simulation
%   starts, driven by the model's own position. With RUNIT true the model is
%   simulated with the viewer and must still pass the T01 self-test, which
%   shows the scene did not change the result.
%
%   Needs UAV Toolbox and its Simulation 3D blocks. They are found by block
%   name, because the library that holds them has moved between releases.
%   If a block or a port is not as expected the function stops and prints
%   what the installed release offers; it does not guess.
%
%   NOT RUN BEFORE DELIVERY. Written from the documented block interface:
%   Simulation 3D Scene Configuration (no ports) and Simulation 3D UAV
%   Vehicle with Translation and Rotation inputs sized by vehicle type.
%   Expect to adjust the input sizes for the installed release.
%
%   The scene is the packaged 'US City Block'. It is a picture of where the
%   aircraft is. It is not a farm, and nothing here is detection evidence.

if nargin < 3
    runIt = true;
end
source = 'uav_t01_mission_r2025b';
load_system(fullfile(r2025bDir, [source '.slx']));
model = 'uav_t01_ue';
if bdIsLoaded(model)
    close_system(model, 0);
end
if ~exist(outDir, 'dir')
    mkdir(outDir);
end
result.model = fullfile(outDir, [model '.slx']);
save_system(source, result.model);   % the loaded diagram is now uav_t01_ue

scene = findBlock('Simulation 3D Scene Configuration');
vehicle = findBlock('Simulation 3D UAV Vehicle');
add_block(scene, [model '/Scene'], 'Position', [60 520 220 580]);
add_block(vehicle, [model '/UAV in scene'], 'Position', [520 500 700 600]);
ports = get_param([model '/UAV in scene'], 'PortHandles');
if numel(ports.Inport) ~= 2
    names = fieldnames(get_param([model '/UAV in scene'], 'DialogParameters'));
    error('uav:ue:ports', ['The Simulation 3D UAV Vehicle block in this release has %d inputs, not the two ' ...
        '(Translation, Rotation) this script wires. Its parameters are: %s'], numel(ports.Inport), sprintf('%s ', names{:}));
end

% Position in the scene frame: X north, Y east, Z down; 40 m above ground.
av = 'WBS 1.1 Air Vehicle';
uav_add(model, 'Constant', 'height', 'Value', '-40');
uav_add(model, 'Mux', 'position mux', 'Inputs', '3');
add_line(model, [av '/2'], 'position mux/1', 'autorouting', 'on');
add_line(model, [av '/1'], 'position mux/2', 'autorouting', 'on');
add_line(model, 'height/1', 'position mux/3', 'autorouting', 'on');
uav_add(model, 'Constant', 'attitude', 'Value', '[0 0 0]');
add_line(model, 'position mux/1', 'UAV in scene/1', 'autorouting', 'on');
add_line(model, 'attitude/1', 'UAV in scene/2', 'autorouting', 'on');
save_system(model);

result.ran = false;
if runIt
    r = uav_t01_run(model);
    for k = 1:numel(r.lines)
        fprintf('%s\n', r.lines{k});
    end
    result.selftest = r;
    result.ran = true;
    if ~r.pass
        error('uav:ue:selftest', 'The model with the Unreal scene fails the T01 self-test.');
    end
    fprintf('UAV_T01_UE_PASS release=%s\n', version('-release'));
else
    fprintf('UAV_T01_UE_BUILT model=%s (not simulated)\n', result.model);
end
end

function path = findBlock(name)
libraries = {'uavsim3dlib', 'uavlib', 'sim3dlib', 'aerolibsim3d', 'drivingsim3d'};
tried = '';
for k = 1:numel(libraries)
    try
        load_system(libraries{k});
    catch %#ok<CTCH>
        continue;
    end
    tried = [tried ' ' libraries{k}]; %#ok<AGROW>
    found = find_system(libraries{k}, 'LookUnderMasks', 'all', 'FollowLinks', 'on', 'Name', name);
    if ~isempty(found)
        path = found{1};
        return;
    end
end
error('uav:ue:block', 'Block "%s" was not found. Libraries that loaded:%s. Check that UAV Toolbox is installed.', name, tried);
end
