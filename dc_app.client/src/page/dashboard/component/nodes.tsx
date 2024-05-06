import React, { useState } from 'react'
import { useNavigate } from "react-router-dom"

import './nodes.css'
import spreadsheetIcon from '../media/spreadsheet_150px.png'

import { SpreadshMetaModel } from '../../../model/model'

import Card from 'react-bootstrap/Card'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'
import axios from 'axios'
import { api_url } from '../../../utilities/api'
import DropdownToggle from 'react-bootstrap/esm/DropdownToggle'
import SharePopup from './share'

interface SpreadsheetNodeListProps {
  smodels:SpreadshMetaModel[];
  setSmodels:React.Dispatch<React.SetStateAction<SpreadshMetaModel[]>>;
}

const SpreadsheetNodeList = ({smodels, setSmodels}:SpreadsheetNodeListProps) => {

  return (
    <div className='SpreadsheetNodeListDiv'>
      {smodels.map(function (smodel, index) { 
        return (
          <SpreadsheetCard key={index} smodel={smodel} smodels={smodels} setSmodels={setSmodels}/>
      )
      })}
    </div>
  )
}
export default SpreadsheetNodeList;

interface SpreadsheetNodeProps {
  smodel:SpreadshMetaModel;
  smodels:SpreadshMetaModel[];
  setSmodels: React.Dispatch<React.SetStateAction<SpreadshMetaModel[]>>;
}

const SpreadsheetCard = ({smodel, smodels, setSmodels}:SpreadsheetNodeProps) => {

  const navigate = useNavigate();

  const handleDelete = (e: React.FormEvent, id: string) => {
    e.preventDefault();

    axios({
      method: 'delete',
      url: `${api_url}/api/spreadsheet/${id}`,
      withCredentials: true
    }).then(() => {
      setSmodels(smodels.filter((smodel) => smodel.id !== id));

    }).catch(error => {
      console.error(error)
      return;
    });
  }

  function handleDowload(e: React.FormEvent, id: string) {
    e.preventDefault();

    fetch(`${api_url}/api/spreadsheet/${id}/download`, {
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

  const [sharePopup, setSharePopup] = useState<boolean>(false);
  function handleSharePopup(e: React.FormEvent) {
    e.preventDefault();

    setSharePopup(true);
  }

  return (
    <Card className='spreadsheetCard'>
      
      <Card.Body className='cardBody'> 
        <EditableName smodel={smodel} smodels={smodels} setSmodels={setSmodels} />
        <Card.Img variant="top" src={spreadsheetIcon} />

        <div className="cardButtonsDiv">
          <Button variant="outline-primary" onClick={() => navigate(`/spreadsheet/${smodel.id}`)}>Open</Button>

          <Dropdown drop="down-centered" align="end" title="">
            <DropdownToggle variant="outline-primary">
            </DropdownToggle>

            <Dropdown.Menu>
              <Dropdown.Item onClick={(e) => handleDelete(e, smodel.id)}>Delete</Dropdown.Item>
              <Dropdown.Item onClick={(e) => handleDowload(e, smodel.id)}>Download</Dropdown.Item>
              <Dropdown.Item onClick={(e) => handleSharePopup(e)}>Share</Dropdown.Item>
            </Dropdown.Menu>

          </Dropdown>
        </div>
      </Card.Body>

      <SharePopup sharePopupBool={sharePopup} setSharePopupBool={setSharePopup} smodel={smodel }/>
    </Card>
  )
}

interface EditableNameProps {
  smodel:SpreadshMetaModel;
  smodels:SpreadshMetaModel[];
  setSmodels:React.Dispatch<React.SetStateAction<SpreadshMetaModel[]>>;
}

const EditableName = ({smodel, smodels, setSmodels}:EditableNameProps) => {
  
  const [editBool, setEditBool] = useState<boolean>(false);
  const [editName, setEditName] = useState<string>(smodel.name);

  const handleEnter = (e:React.KeyboardEvent<HTMLInputElement>, id:string) => {

    if(e.key === 'Enter' && editName !== "") {
      setSmodels(smodels.map((smodel) => (
        smodel.id === id ? {...smodel, name:editName} : smodel
      )))
      setEditBool(false);

      fetch(`${api_url}/api/spreadsheet/${smodel.id}/name/${editName}`, {
        method: "PUT",
        credentials: "include"
      }).then(resolved => {
        if (!resolved.ok) {
          throw new Error("PUT edit name not ok");
        }
      });
    }
  };

  return (
    editBool ? (
      <input
      value={editName} 
      onKeyDown={(e) => handleEnter(e, smodel.id) }
      onChange={(e) => setEditName(e.target.value)} 
      onBlur={() => setEditBool(false)}
      className='nodeNameInput'
      autoFocus/>
    ) : (
      <div 
        onClick={() => {setEditBool(true)}} 
        className='nodeNameSpan'>{smodel.name}
      </div>
    )
  )
}
