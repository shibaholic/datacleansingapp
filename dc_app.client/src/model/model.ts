export interface SpreadshMetaModel {
  name:string;
  id:string;
  creation_date:string;
  type:string;
  total: number;
}
export interface SpreadshColModel {
  col_id: number;
  col_order: number;
  name: string;
  col_type: string;
}

export interface PreloadData {
  spreadsheetConfig: SpreadshMetaModel;
  columnConfigs: SpreadshColModel[];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  rowData: any[];
}

export interface UserHasSpreadsheet {
  username: string;
  permission: string;
}

export interface LongPollMessage {
  message: string;
  object?: object;
}