function value = get_param(path, name)
global UAVMOCK
if isKey(UAVMOCK.diagrams, path)
    d = UAVMOCK.diagrams(path);
    switch name
        case 'Type'
            value = 'block_diagram';
        case 'FileName'
            value = d.file;
        otherwise
            value = '';
            for k = 1:2:numel(d.params)
                if strcmp(d.params{k}, name)
                    value = d.params{k + 1};
                end
            end
    end
elseif isKey(UAVMOCK.blocks, path)
    switch name
        case 'Type'
            value = 'block';
        case 'BlockType'
            value = UAVMOCK.blocks(path).type;
        otherwise
            value = uavmock_param(path, name);
    end
else
    error('mock:get_param', '%s does not exist.', path);
end
end
