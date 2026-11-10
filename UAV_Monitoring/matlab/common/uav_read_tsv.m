function t = uav_read_tsv(path)
%UAV_READ_TSV  Read a tab-separated file with a header row.
%   T = UAV_READ_TSV(PATH) returns a struct with one field per header name,
%   each an N-by-1 cell array of strings. Uses only functions in R2011a.

fid = fopen(path, 'r');
if fid < 0
    error('uav:tsv:open', 'Cannot open "%s".', path);
end
header = {};
rows = {};
while true
    line = fgetl(fid);
    if ~ischar(line)
        break;
    end
    if ~isempty(line) && line(end) == char(13)
        line = line(1:end - 1);
    end
    if isempty(strtrim(line))
        continue;
    end
    fields = regexp(line, '\t', 'split');
    if isempty(header)
        header = fields;
    else
        if numel(fields) ~= numel(header)
            fclose(fid);
            error('uav:tsv:row', '%s: expected %d fields, found %d in "%s".', path, numel(header), numel(fields), line);
        end
        rows(end + 1, :) = fields; %#ok<AGROW>
    end
end
fclose(fid);
t = struct();
for k = 1:numel(header)
    if isempty(rows)
        t.(header{k}) = cell(0, 1);
    else
        t.(header{k}) = rows(:, k);
    end
end
end
