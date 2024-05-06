using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.Entities;

public class UploadStatusEntity
{
    public string uploadId { get; set; }
    public string status { get; set; } // can be "waiting", "uploading", "completed"
    public int? value_percent { get; set; } // when status == "uploading" then it is the percentage for that
    public string? location { get; set; } // when status == "completed" then this points to the newly uploaded spreadsheet's location
}
