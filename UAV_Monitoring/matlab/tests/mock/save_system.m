function save_system(name, file)
global UAVMOCK
d = UAVMOCK.diagrams(name);
d.file = file;
UAVMOCK.diagrams(name) = d;
uavmock_check(name);
end
