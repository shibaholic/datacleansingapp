import Form from 'react-bootstrap/Form';
import Button from 'react-bootstrap/Button'
import CloseButton from 'react-bootstrap/CloseButton';
import { PreloadData } from '../../../model/model'
import './../SpreadsheetPageStyle.css'
import { useEffect, useState } from 'react';
import { api_url } from '../../../utilities/api';


interface SelectField {
  select_id: number;
  col_id: number | string;
}

interface SelectsComponentProps {
  name: string;
  select_type: string; // "key" columns are the ones that are deduplicated. "associate" columns are the ones that 'tag' along with the key column, but are not deduplicated
  label: string;
  updateFields: React.Dispatch<React.SetStateAction<SelectField[]>>;
  preloadData: PreloadData;
}
const SelectsComponent = ({ select_type, label, updateFields, preloadData }: SelectsComponentProps) => {
  function generateOptions() {
    const options = [<option key="0" value="0" disabled>Unselected</option>];
    for (let i = 0; i < preloadData.columnConfigs.length; i++) {
      options.push((
        <option
          key={i + 1}
          value={preloadData.columnConfigs[i].col_id.toString()}
        >
          {preloadData.columnConfigs[i].name}
        </option>))
    }
    return options;
  }

  function onSelectChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const selectId = Number(e.target.id.substring(9)); // each Form.Select has a "key" attribute that takes the "selectKey#" format
    const new_optionId = Number(e.target.value);

    // col_key -> col_key
    const temp = [...selectFields];
    temp.splice(selectId - 1, 1, { select_id: selectId, col_id: new_optionId });
    setSelectFields(temp);
    console.log("selectChange replaced s_id:" + selectId + " option_id:" + new_optionId);

    console.log("selectFields");
    console.log(selectFields);
  }

  function createSelect() {
    console.debug("createSelect");
    console.debug(selectFields.length);
    setSelectFields([...selectFields, { select_id: selectFields.length + 1, col_id: "0" }]);
  }

  function deleteSelect() {
    console.log("deleteSelect");
    const temp = selectFields;
    temp.pop();
    setSelectFields([...temp]);
  }

  const [selectFields, setSelectFields] = useState<SelectField[]>([]);

  useEffect(() => { // using useEffect so that the parent component's fields are updated on the same step as when selectFields is updated.
    updateFields(selectFields);
  }, [selectFields]);

  return (
    <div style={{marginBottom:"1rem"} }>
      <Form.Label>{label}</Form.Label>
      {selectFields.map((selectField:SelectField, index:number) => {
        return (
          <div key={index} className="SelectKeyDiv">
            <span style={{ position: 'relative', top: '6px' }}>{selectField.select_id}</span>
            <Form.Select id={"selectKey" + selectField.select_id} onChange={onSelectChange} value={selectField.col_id}>
              {generateOptions()}
            </Form.Select>
            {selectField.select_id === (selectFields.length) ?
              <Button variant="outline-dark" onClick={() => deleteSelect()}>X</Button>
              : <></>
            }
          </div>
        )
      })}
      <Button onClick={() => createSelect()} style={{ display: 'block', margin: 'auto', width: 'fit-content', marginTop: '10px' }}>Add {select_type} column</Button>
    </div>
  )
}

interface IDedupPanel {
  preloadData: PreloadData;
  dedupBool: boolean;
  setDedupBool: React.Dispatch<React.SetStateAction<boolean>>;
}
const DedupPanel = ({ preloadData, setDedupBool }: IDedupPanel) => {

  const handleClose = () => {
    setDedupBool(false);
  }

  const [isProcessing, setProcessing] = useState<boolean>(false);
  const [linkToDedup, setLinkToDedup] = useState<string>(""); // outputs a link to the most recent newly deduplicated spreadsheet
  function submitDeduplicationForm() {
    setProcessing(true);
    setLinkToDedup("");
    
    const url_id = preloadData.spreadsheetConfig.id;
    const number_of_cols_to_deduplicate = dedupFields.length;
    const deduplicate_col_ids: string[] = [];
    dedupFields.forEach((dedupField) => {
      deduplicate_col_ids.push(dedupField.col_id.toString());
    })
    // TODO: client-side validation that keyCols are all different

    const bodyFormData = new FormData();
    for (let i = 0; i < number_of_cols_to_deduplicate; i++) {
      bodyFormData.append('dedup_col_ids', deduplicate_col_ids[i]);
    }
    for (let i = 0; i < assocFields.length; i++) {
      bodyFormData.append('assoc_col_ids', assocFields[i].col_id.toString());
    }

    fetch(`${api_url}/api/datacleanse/${url_id}/deduplicate`, {
      method: "POST",
      credentials: "include",
      body: bodyFormData
    }).then((resolved) => {
      if (resolved.ok) {
        return resolved.json();
      }
    }).then((deduplicateResult) => {
      // once deduplicateResult is received, make a link to go to the new spreadsheet if the browser does not let the new window be opened.
      setProcessing(false);
      const linkToDedup = `/spreadsheet/${deduplicateResult.newSpreadsheetId}`;
      setLinkToDedup(linkToDedup);
      window.open(linkToDedup, "_blank");
    })
  }

  const [dedupFields, setDedupFields] = useState<SelectField[]>([]);
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const [assocFields, setAssocFields] = useState<SelectField[]>([]);

  return (
    <div className="DedupPanel">
      <div className="DedupPanel-header">
        <h4>Deduplication</h4>
        <CloseButton onClick={handleClose} style={{position:'absolute', right:'13px'} }/>
      </div>
      <Form className="DedupPanel-form">
        <SelectsComponent name="dedupSelect" select_type="deduplicate" label="Select columns to dedup" updateFields={setDedupFields} preloadData={preloadData} />
        <SelectsComponent name="assocSelect" select_type="associate" label="Select columns to associate" updateFields={setAssocFields} preloadData={preloadData} />
      </Form>
      <div className="DedupPanel-footer">
        <Button onClick={!isProcessing ? submitDeduplicationForm : undefined} style={{ float: 'right', marginRight: '20px' }} disabled={isProcessing}>
          {isProcessing ? "processing" : "Submit"}
        </Button>
        {linkToDedup !== "" ? 
          <a href={linkToDedup}>If you are not redirected, click here for the results</a>
          :
          <></>
        }
      </div>
    </div>
  );
}

export { DedupPanel };