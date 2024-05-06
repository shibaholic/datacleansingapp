using DocumentFormat.OpenXml.Spreadsheet;
using ServiceLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.ServiceLayer;

public interface IDataCleansingService
{
    Task<List<List<string>>> DeduplicateAlgorithm(uint spreadsheetId, int[] dedup_col_ids, int[] assoc_col_ids);
    Task<int> AddReviewColumns(uint spreadsheetId);
}

public class DataCleansingService:IDataCleansingService
{
    private readonly ISpreadsheetDataService _spreadsheetDataService;
    private readonly ISpreadsheetConfigService _spreadsheetConfigService;
    public DataCleansingService(ISpreadsheetDataService spreadsheetDataService, ISpreadsheetConfigService spreadsheetConfigService)
    {
        _spreadsheetDataService = spreadsheetDataService;
        _spreadsheetConfigService = spreadsheetConfigService;
    }

    public async Task<List<List<string>>> DeduplicateAlgorithm(uint spreadsheetId, int[] dedup_col_ids, int[] assoc_col_ids)
    {
        // 2.1 get all values in each dedup col
        var dedup_col_data = (await _spreadsheetDataService.ReadCellDataByCol((uint)spreadsheetId, dedup_col_ids)).ToList(); // TODO: check nullability in the validation checks

        // convert List of DapperRow [{col1 = '1', col2= '2'}, {col1='3', col2='4'} ...]
        // into List of List [['1', '2'], ['3', '4'], ['1', '2'], ['3', '4']]
        var dedup_values_gridlist = new List<List<string>>();
        
        //Console.WriteLine("dedup_col_data unpack");
        foreach (var dapperRow in dedup_col_data)
        {
            var row_dict = ((IDictionary<string, object>)dapperRow);
            List<string> list_of_col_values = new List<string>();
            int col_id_counter = 0;
            foreach(var keyValuePair in row_dict)
            {
                if (dedup_col_ids.Contains(Int32.Parse(keyValuePair.Key.Substring(3)))) {
                    list_of_col_values.Add(keyValuePair.Value.ToString());
                    //Console.Write(keyValuePair.Key.ToString() + ":" + keyValuePair.Value.ToString() + " ");
                }
            }
            //Console.WriteLine();
            dedup_values_gridlist.Add(list_of_col_values);
        }

        // 2.2 get all values in each assoc col
        var assoc_col_data = (await _spreadsheetDataService.ReadCellDataByCol((uint)spreadsheetId, assoc_col_ids)).ToList();

        // convert List of DapperRow [{col1 = '1', col2= '2'}, {col1='3', col2='4'} ...]
        // into List of List [col1=['1', '2', '3', '4'], col2=['1', '2', '3', '4']]
        var assoc_values_gridlist = new List<List<string>>();
        
        //Console.WriteLine("assoc_col_data unpack");
        foreach (var dapperRow in assoc_col_data)
        {
            var row_dict = ((IDictionary<string, object>)dapperRow);
            List<string> list_of_col_values = new List<string>();
            int col_id_counter = 0;
            foreach (var keyValuePair in row_dict)
            {
                if (assoc_col_ids.Contains(Int32.Parse(keyValuePair.Key.Substring(3))))
                {
                    list_of_col_values.Add(keyValuePair.Value.ToString());
                    //Console.Write(keyValuePair.Key.ToString() + ":" + keyValuePair.Value.ToString() + " ");
                }
            }
            //Console.WriteLine();
            assoc_values_gridlist.Add(list_of_col_values);
        }

        // splices the string " " spaces into multiple strings
        var dedup_spliced_values = new List<List<string>>();
        for (int i = 0; i < dedup_values_gridlist.Count; i++)
        {
            var spliced_row = new List<string>();
            for (int j = 0; j < dedup_values_gridlist[i].Count; j++)
            {
                spliced_row = spliced_row.Concat(dedup_values_gridlist[i][j].Split(" ")).ToList();
            }
            dedup_spliced_values.Add(spliced_row);
        }

        // 3. loop through arraylists in dedup_spliced_values
        var dedup_results = new List<List<string>>();
        var assoc_results = new List<List<string>>();
        for (var i = 0; i < dedup_spliced_values.Count; i++)
        {
            var res_indexes = new List<int>();

            // loop through values inside 1 dedup_spliced_values row
            for (var j = 0; j < dedup_spliced_values[i].Count; j++)
            {
                var value = dedup_spliced_values[i][j];
                // loop through results, to see if this value is already in results
                for (int result_index = 0; result_index < dedup_results.Count; result_index++)
                {
                    var one_result_row = dedup_results[result_index];
                    if (one_result_row.Contains(value))
                    {
                        res_indexes.Add(result_index);
                    }
                }
            }

            res_indexes = res_indexes.Distinct().ToList();

            if (res_indexes.Count == 0)
            {
                dedup_results.Add(dedup_spliced_values[i]);
                assoc_results.Add(assoc_values_gridlist[i]);
            }
            else // means this cols_combined[i] is connected to other values already in the results
            {
                // add all other results[res_index]s to the first res_index's result
                int first_resIndex = res_indexes[0];
                var list_of_other_resultIndexes = new List<int>();
                for (int j = 1; j < res_indexes.Count; j++)
                {
                    list_of_other_resultIndexes.Add(res_indexes[j]);
                }

                // add dedup_spliced_values[i] to that first res_index's result
                dedup_results[first_resIndex] = dedup_results[first_resIndex].Concat(dedup_spliced_values[i]).Distinct().ToList();

                for(int x = 0; x < assoc_values_gridlist[i].Count; x++)
                {
                    assoc_results[first_resIndex][x] = assoc_results[first_resIndex][x] + " " + assoc_values_gridlist[i][x];
                }

                if (list_of_other_resultIndexes.Count >= 1) // 1 or more other resultIndexes. Then 
                {
                    // add the results[otherResultIndex] to results[first_resIndex]
                    foreach (var otherResultIndex in list_of_other_resultIndexes)
                    {
                        dedup_results[first_resIndex] = dedup_results[first_resIndex].Concat(dedup_results[otherResultIndex]).Distinct().ToList();

                        for (int x = 0; x < assoc_values_gridlist[i].Count; x++)
                        {
                            assoc_results[first_resIndex][x] = assoc_results[first_resIndex][x] + " " + assoc_results[otherResultIndex][x];
                        }
                    }
                    // delete the old results[otherResultIndex]
                    foreach (var otherResultIndex in list_of_other_resultIndexes)
                    {
                        dedup_results.RemoveAt(otherResultIndex);
                        assoc_results.RemoveAt(otherResultIndex);
                    }
                }
            }
        }

        var dedup_assoc_result = new List<List<string>>();

        for(int i = 0; i < dedup_results.Count; i++)
        {
            List<string> row = new List<string>();
            string dedup_combined_string = "";
            foreach(var str in dedup_results[i])
            {
                dedup_combined_string += str + " ";
            }
            row.Add(dedup_combined_string); // dedup is always the first col

            foreach(string assoc_value in assoc_results[i])
            {
                row.Add(assoc_value);
            }

            dedup_assoc_result.Add(row);
        }

        return dedup_assoc_result;
    }

    public async Task<int> AddReviewColumns(uint spreadsheetId)
    {
        // 1. insert into column config the 3 boolean columns and 1 text column
        var currentColConfs = await _spreadsheetConfigService.ReadColumnConfig(spreadsheetId);
        int highest_col_id = currentColConfs.Select(colConf => colConf.col_id).Max();

        Console.WriteLine("highest_col_id:" + highest_col_id.ToString());

        var newColConfs = new List<ColumnConfig>();
        for(int i = 1; i < 4; i++)
        {
            string col_name_web;
            if (i == 1) col_name_web = "Correct";
            else if (i == 2) col_name_web = "Incorrect";
            else col_name_web = "Review later";
            newColConfs.Add(new ColumnConfig() { spr_id = spreadsheetId, col_id = highest_col_id + i, col_order = highest_col_id + i, col_name_web = col_name_web, col_type = "boolean" });
        }
        newColConfs.Add(new ColumnConfig() { spr_id = spreadsheetId, col_id = highest_col_id + 4, col_order = highest_col_id + 4, col_name_web = "Comment", col_type = "stringMax" });

        int colConfRowsAffected = 0;
        foreach(var colConf in newColConfs)
        {
            colConfRowsAffected += await _spreadsheetConfigService.InsertColumnConfig(colConf);
        }
        Console.WriteLine("colConfRA: " + colConfRowsAffected);

        // 2. alter table dynamic

        var spreadsheetConfig = await _spreadsheetConfigService.ReadSpreadsheetConfig(spreadsheetId);

        int alterTableSomething = await _spreadsheetDataService.AlterTableAddColumn(spreadsheetConfig.dynamic_table_name, newColConfs);

        Console.WriteLine("alterTable: " + alterTableSomething);

        return 1;
    }
}
