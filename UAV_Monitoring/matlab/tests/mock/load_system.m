function load_system(file)
global UAVMOCK
[~, name] = fileparts(file);
if ~isKey(UAVMOCK.diagrams, name)
    error('mock:load_system', '%s is not built in this mock session.', name);
end
end
