import json

FILENAME_IN = "aggregated.tsv"
FILENAME_OUT = "aggregated.unjson.tsv"
JSON_FIELD = "game_sim_settings"

def main():
    with open(FILENAME_IN) as f:
        lines = f.readlines()
    field_names = lines[0].strip().split("\t")
    data0 = lines[1].strip().split('\t')
    json_field_index = field_names.index(JSON_FIELD)
    parsed_data0 = json.loads(json.loads(data0[json_field_index]))
    field_names.remove(JSON_FIELD)
    field_names += list(parsed_data0.keys())

    data_out = []
    for i, l in enumerate(lines):
        if i == 0:
            continue
        line_data = l.strip().split("\t")
        parsed_data = json.loads(json.loads(line_data[json_field_index]))
        line_data.pop(json_field_index)
        line_data += parsed_data.values()
        #print(line_data)
        data_out.append(line_data)

    with open(FILENAME_OUT, 'w') as f:
        f.write("\t".join(field_names) + "\n")
        for l in data_out:

            f.write("\t".join(str(i) for i in l) + "\n")





if __name__ == "__main__":
    main()
