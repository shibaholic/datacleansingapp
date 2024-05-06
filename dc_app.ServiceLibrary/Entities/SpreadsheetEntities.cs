using AutoMapper;
using AutoMapper.Configuration.Annotations;

namespace ServiceLibrary.Entities;

public class SpreadsheetConfig {
    public uint id { get; set; }
    public string name { get; set; }
    public string url_id { get; set; }
    public DateTimeOffset creation_date { get; set; }
    public string type_spreadsheet { get; set; }
    public int total { get; set; }
    public string dynamic_table_name { get; set; }
    
    // add the one-to-many relationships with UserGuestSpreadsheet and UserOwnsSpreadsheet
}
public class SpreadsheetConfigDto
{
    public string name { get; set; }
    public string id { get; set; }
    public DateTimeOffset creation_date { get; set; }
    public string type_spreadsheet { get; set; }
    public uint total { get; set; }
}

public class ColumnConfig
{
    public uint spr_id { get; set; }
    public int col_id { get; set; }
    public int col_order { get; set; }
    public string col_name_web { get; set; }
    public string col_type { get; set; }
    // TODO: implement ordering, column width. 
}

public class ColumnConfigDto
{
    public int col_id { get; set; }
    public int col_order { get; set; }
    public string name { get; set; }
    public string col_type { get; set; }
}

public class UserHasSpreadsheet
{
    public Guid usr_id {get; set;}
    public string? username { get; set;}
    public uint spr_id { get; set;}
    public string? permission { get; set; }
}

public class UserHasSpreadsheetDto
{
    public string? username { get; set; }
    public string? permission { get; set; }
}

public class LongPollMessage
{
    public string message { get; set; }
    public object? data { get; set; }
}