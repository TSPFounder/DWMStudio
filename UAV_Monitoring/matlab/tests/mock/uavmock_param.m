function value = uavmock_param(path, name)
%UAVMOCK_PARAM  Last value given for a block parameter, or '' when never set.
global UAVMOCK
params = UAVMOCK.blocks(path).params;
value = '';
for k = 1:2:numel(params)
    if strcmp(params{k}, name)
        value = params{k + 1};
    end
end
end
