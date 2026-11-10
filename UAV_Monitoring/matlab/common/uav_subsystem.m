function path = uav_subsystem(parent, name, inports, outports, tag, description)
%UAV_SUBSYSTEM  Add an empty Subsystem block with named ports.
%   PATH = UAV_SUBSYSTEM(PARENT, NAME, INPORTS, OUTPORTS, TAG, DESCRIPTION)
%   adds Subsystem NAME under PARENT with one Inport per name in INPORTS and
%   one Outport per name in OUTPORTS, in that order. TAG (for example
%   'WBS 1.1.1') is stored in the block's Tag and shown under the block, so
%   the WBS code survives a rename.

path = uav_add(parent, 'SubSystem', name);
set_param(path, 'Tag', tag, 'Description', description, 'AttributesFormatString', '%<Tag>');
for k = 1:numel(inports)
    uav_add(path, 'Inport', inports{k});
end
for k = 1:numel(outports)
    uav_add(path, 'Outport', outports{k});
end
end
