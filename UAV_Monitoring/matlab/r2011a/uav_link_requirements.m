function report = uav_link_requirements(packageRoot, modelDir, pdqRmiDir)
%UAV_LINK_REQUIREMENTS  Link WBS subsystems to their requirements (Simulink V&V, R2011a).
%   REPORT = UAV_LINK_REQUIREMENTS(PACKAGEROOT, MODELDIR, PDQRMIDIR)
%
%   For every row of baseline/requirement_allocation.tsv the WBS subsystem
%   in uav_wbs_lib gets three requirement links:
%     pdq    PD Quick requirement, by RequirementID      (rmipdqlinktype)
%     Excel  the workbook PD Quick exported, by the same ID (linktype_rmi_excel)
%     Word   the requirements specification, by bookmark   (linktype_rmi_word)
%
%   Before running:
%     1. Seed the PD Quick project and run the seeder's export command into
%        MODELDIR\requirements. That writes uav_farm.pdqreq, uav_farm.xlsx
%        and requirement_ids.tsv from the project database.
%     2. Copy documents\UAV_Requirements_Specification.docx into
%        MODELDIR\requirements.
%     3. Build the library with uav_r2011a_build_and_test.
%   PDQRMIDIR is PD Quick's PDQStudio\tools\rmi folder.
%
%   Running it again adds nothing: an existing link is left alone.
%   Writes MODELDIR\uav_wbs_lib_links.tsv with pdq_collect_links and returns
%   counts in REPORT. Prints UAV_LINKS_OK on success.

addpath(pdqRmiDir);
rmi('register', 'rmipdqlinktype');
reqDir = fullfile(modelDir, 'requirements');
catalog = fullfile('requirements', 'uav_farm.pdqreq');
workbook = fullfile('requirements', 'uav_farm.xlsx');
document = fullfile('requirements', 'UAV_Requirements_Specification.docx');
needed = {'uav_farm.pdqreq', 'uav_farm.xlsx', 'requirement_ids.tsv', 'UAV_Requirements_Specification.docx'};
for k = 1:numel(needed)
    if exist(fullfile(reqDir, needed{k}), 'file') ~= 2
        error('uav:link:missing', '%s is missing from %s. See the help of uav_link_requirements.', needed{k}, reqDir);
    end
end
ids = uav_read_tsv(fullfile(reqDir, 'requirement_ids.tsv'));
alloc = uav_read_tsv(fullfile(packageRoot, 'baseline', 'requirement_allocation.tsv'));

lib = 'uav_wbs_lib';
load_system(fullfile(modelDir, [lib '.mdl']));
set_param(lib, 'Lock', 'off');
report = struct('pdq', 0, 'excel', 0, 'word', 0, 'existing', 0, 'rows', numel(alloc.RequirementNumber));
for k = 1:numel(alloc.RequirementNumber)
    number = alloc.RequirementNumber{k};
    hit = find(strcmp(ids.RequirementNumber, number), 1);
    if isempty(hit)
        error('uav:link:unknown', 'Requirement %s is not in requirement_ids.tsv. Seed and export again.', number);
    end
    id = ids.RequirementID{hit};
    block = [lib '/' alloc.BlockPath{k}];
    if pdq_link(block, catalog, id)
        report.pdq = report.pdq + 1;
    else
        report.existing = report.existing + 1;
    end
    if pdq_link(block, catalog, id, 'excel', workbook)
        report.excel = report.excel + 1;
    else
        report.existing = report.existing + 1;
    end
    if wordLink(block, document, number, alloc.Title{k})
        report.word = report.word + 1;
    else
        report.existing = report.existing + 1;
    end
end
save_system(lib);
report.linksFile = fullfile(modelDir, [lib '_links.tsv']);
pdq_collect_links(fullfile(modelDir, [lib '.mdl']), report.linksFile);
fprintf('UAV_LINKS_OK rows=%d pdq=%d excel=%d word=%d already_present=%d report=%s\n', ...
    report.rows, report.pdq, report.excel, report.word, report.existing, report.linksFile);
end

function added = wordLink(block, document, number, title)
% Word link to the bookmark named after the requirement (SR-T01-01 -> SR_T01_01).
location = ['@' strrep(number, '-', '_')];
existing = rmi('get', block);
for j = 1:numel(existing)
    if strcmp(existing(j).reqsys, 'linktype_rmi_word') && strcmp(existing(j).id, location)
        added = false;
        return;
    end
end
req = rmi('createempty');
req.reqsys = 'linktype_rmi_word';
req.doc = document;
req.id = location;
req.description = [number ' ' title];
req.linked = true;
rmi('cat', block, req);
added = true;
end
