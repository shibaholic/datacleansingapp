import { useEffect, useMemo, useRef, useState } from "react"
import { SpreadsheetLoaderResponse } from "./SpreadsheetPageLoader";
import { useLoaderData } from "react-router-dom";
import Dropdown from "react-bootstrap/esm/Dropdown";
import DropdownButton from 'react-bootstrap/DropdownButton';
import { PreloadData } from "../../model/model";

import DataGrid, { SelectCellFormatter, textEditor } from 'react-data-grid';
import type { Column, RowsChangeData } from 'react-data-grid'
import 'react-data-grid/lib/styles.css';
import './SpreadsheetPageStyle.css';

import axios from "axios";
import { DedupPanel } from "./component/DeduplicationPanel";
import { api_url } from "../../utilities/api";
import { longPollSync } from "../../utilities/poll";
import { useAuth } from "../../utilities/AuthContext";

const SpreadsheetPage = () => {
  const [dedupBool, setDedupBool] = useState<boolean>(false);
  function deduplicationButton() {
    setDedupBool(true);
  }

  function addReviewColButton() {
    fetch(`${api_url}/api/datacleanse/${spreadsheetMetadata.spreadsheetConfig.id}/addreviewcol`, {
      method: "POST",
      credentials: "include"
    }).then(resolved => {
      if (!resolved.ok) {
        throw new Error("addreviewcol response was not ok");
      }
      return;
    }).then(() => {
      window.location.reload();
    });
  }

  function Export() {
    fetch(`${api_url}/api/spreadsheet/${spreadsheetMetadata.spreadsheetConfig.id}/download`, {
      method: "GET",
      credentials: "include"
    }).then(resolved => {
      if (!resolved.ok) {
        throw new Error("download api response was not ok");
      }
      return resolved.blob();
    }).then(blob => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.style.display = "none";
      a.href = url;
      a.download = "spreadsheet.xlsx";
      document.body.appendChild(a);
      a.click();
      a.remove();
    });
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const spreadsheetMetadata: PreloadData = useLoaderData() as SpreadsheetLoaderResponse;

  
  return (
    <div className="root">
      <div className="SpreadshHeaderDiv">
        <h2 style={{ fontSize:'30px' }}>{spreadsheetMetadata.spreadsheetConfig.name}</h2>
        <ul className="nav nav-pills" style={{ gap: '8px', marginBlockEnd: '8px' } }>
          <li className="nav-item">
            <DropdownButton title="File" className="dropdown-file" variant="secondary" size="sm">
              <Dropdown.Item eventKey="2">Save as</Dropdown.Item>
              <Dropdown.Item eventKey="3" onClick={Export}>Export (xlsx)</Dropdown.Item>
            </DropdownButton>
          </li>
          <li className="nav-item">
            <DropdownButton title="Operations" className="dropdown-file" variant="secondary" size="sm">
              <Dropdown.Item eventKey="1" onClick={deduplicationButton}>Deduplicate</Dropdown.Item>
            </DropdownButton>
          </li>
          <li className="nav-item">
            <DropdownButton title="Review" className="dropdown-file" variant="secondary" size="sm">
              <Dropdown.Item eventKey="1" onClick={addReviewColButton}>Add review columns</Dropdown.Item>
            </DropdownButton>
          </li>
        </ul>
      </div>

      <div className="SpreadshBody">
        <div className="SpreadshDataContainer">
          <SpreadshDataGrid preLoadData={spreadsheetMetadata} />
        </div>

        {dedupBool ? (
          <DedupPanel preloadData={spreadsheetMetadata} dedupBool={dedupBool} setDedupBool={setDedupBool} />
          ) : (
            <>
            </>
        )}
      </div>
  </div>
  )
}

interface SpreadshDataGridProps {
  preLoadData: PreloadData;
}

const SpreadshDataGrid = ({ preLoadData }: SpreadshDataGridProps) => {

  const [rowData, setRowData] = useState(preLoadData.rowData);
  const [selectedRows, setSelectedRows] = useState((): ReadonlySet<number> => new Set());
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const stateRef = useRef<any[]>();
  stateRef.current = rowData;

  const { authed } = useAuth();

  const col_num = preLoadData.columnConfigs.length;

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function longPollCallback(data: any, setState: React.Dispatch<React.SetStateAction<any[]>>) {
    const rowData3 = stateRef.current!;

    if (data.message === "long poll timeout") {
      // our own edit on this spreadsheet, so dont do anything
      return;
    }

    if (data.message !== authed) {
      const rowData2 = [...rowData3];
      const rowObj = { ...rowData3[data.data.row - 1], [data.data.col_id]: data.data.value };
      rowData2[data.data.row - 1] = rowObj;

      setState(rowData2);

    }
  }

  useEffect(() => {
    console.log("datagrid start");
    async function startDataGridLongPoll() {

      const path = "/spreadsheet";

      while (window.location.pathname.startsWith(path)) {
        await longPollSync(`${api_url}/api/sync/spreadsheet/${preLoadData.spreadsheetConfig.id}`, authed as string, longPollCallback, setRowData);
      }
    }

    startDataGridLongPoll();
  }, [])

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function updateRowData(rows: any[], data: RowsChangeData<Row>):void {
    const col_id = data.column.key;
    const id_p = data.indexes[0] + 1;
    const new_value = rows[id_p - 1][col_id].toString();
    const url_id = preLoadData.spreadsheetConfig.id;

    axios.patch(`${api_url}/api/spreadsheet/${url_id}/cells`,
      { "id_p": id_p, "col_id": col_id, "new_value": new_value },
      { headers: { 'Content-Type': 'application/json' }, withCredentials: true }
    ).then( () => {
      //console.log("PATCH resolved: " + resolved);
    }).catch(error => {
      console.error("PATCH rowData " + error);
    });

    setRowData(rows);
  }

  type Row = {
    only: string;
    ID_P: string;
    available: boolean;
  }
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function rowKeyGetter(row: any):number {
    return row.ID_P;
  }

  function primitiveToBoolean(value: string | number | boolean | null | undefined): boolean {
    if (typeof value === 'string') {
      return value.toLowerCase() === 'true' || !!+value;  // here we parse to number first
    }

    return !!value;
  }

  const columns = useMemo((): readonly Column<Row>[] => {
    const columns: Column<Row>[] = [];
    const colData = preLoadData.columnConfigs;

    for (let i = 0; i < col_num; i++) {
      if (colData[i].col_type.startsWith("string")) {
        columns.push({
          key: "col" + colData[i].col_id.toString(),
          name: colData[i].name,
          frozen: i < 1,
          width: 100,
          resizable: true,
          renderEditCell: textEditor
        })
      } else {
        columns.push({
          key: "col" + colData[i].col_id.toString(),
          name: colData[i].name,
          width: 50,
          renderCell({ row, onRowChange, tabIndex }) {
            const colId = "col" + colData[i].col_id.toString() as keyof Row;
            const value = primitiveToBoolean(row[colId]);
            return (
              <SelectCellFormatter
                value={value}
                onChange={() => {
                  onRowChange({ ...row, [colId]: !row[colId] });
                }}
                tabIndex={tabIndex}
                disabled={false}
              />
            );
          }
        });
      }
    }
    return columns;
  }, [col_num, preLoadData.columnConfigs])
 
  const gridElement = (
    <DataGrid
      columns={columns}
      rows={rowData}
      rowKeyGetter={rowKeyGetter}
      defaultColumnOptions={{
        //sortable: true,
        resizable: true
      }}
      selectedRows={selectedRows}
      onSelectedRowsChange={setSelectedRows}
      onRowsChange={updateRowData}
      //sortColumns
      //onSortColumnsChange
      //topSummaryRows
      //bottomSummaryRows
      className="DataGridElement"
    />
  )

  return (
    <>
      {gridElement}
    </>
  )
}

export { SpreadsheetPage }

