function tf = bdIsLoaded(name)
global UAVMOCK
tf = isKey(UAVMOCK.diagrams, name);
end
