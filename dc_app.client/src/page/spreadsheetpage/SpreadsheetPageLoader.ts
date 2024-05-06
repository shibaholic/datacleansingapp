/* eslint-disable @typescript-eslint/no-explicit-any */
import axios from "axios"
import { LoaderFunctionArgs } from "react-router-dom"
import { PreloadData } from "../../model/model"
import { api_url } from "../../utilities/api";
// import { SpreadsheetColumn } from "./SpreadsheetPage";

export const spreadsheetPageLoader = async ({ params }: LoaderFunctionArgs): Promise<PreloadData> => {
  const spreadsheetConfig = await axios.get(
    `${api_url}/api/spreadsheet/${params.id}`,
    { withCredentials: true });
  const columnConfigs = await axios.get(
    `${api_url}/api/spreadsheet/${params.id}/columns`,
    { withCredentials: true });

  const row_data: any[] = [];
  const pageStart = 1;
  const perPage = 100;
  const row_axios = await axios.get(
    `${api_url}/api/spreadsheet/${params.id}/cells?pageStart=${pageStart}&perPage=${perPage}`,
    { withCredentials: true }
  );
  row_data.push(...row_axios.data.data);
  console.log("row_axios.data.total_pages: " + row_axios.data.total_pages);
  console.log(row_axios.data);
  for (let i = row_axios.data.page + 1; i < row_axios.data.total_pages; i++) {
    const row_axios2 = await axios.get(
      `${api_url}/api/spreadsheet/${params.id}/cells?pageStart=${i}&perPage=${perPage}`,
      { withCredentials:true });
    row_data.push(...row_axios2.data.data);
  }

  console.log(`row_data Loader`);
  console.log(row_data);

  console.log("col_response Loader");
  console.log(columnConfigs.data);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const response: PreloadData = {
    spreadsheetConfig: spreadsheetConfig.data,
    columnConfigs: columnConfigs.data,
    rowData: row_data
  }
  return response
}

export type SpreadsheetLoaderResponse = Awaited<ReturnType<typeof spreadsheetPageLoader>>