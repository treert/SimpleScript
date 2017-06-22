local a = 1+2;

print(a,1+2,"hello world");

a = {11,22,33}

foreach k,v in a do
	print("foreach k = ", k, "v = ",v)
end

for i = 1,3 do
	print("for i= ",i, " a[i] = ", a[i])
end

local function ipairs(table)
	return function(table, idx)
		idx = idx + 1
		if (table[idx] == nil) then
			return nil
		else
			return idx,table[idx]
		end
	end, table, 0
end

for k,v in ipairs(a) do
	print("foreach k = ", k, "v = ",v)
end