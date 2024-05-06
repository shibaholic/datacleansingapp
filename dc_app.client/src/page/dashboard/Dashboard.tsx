import {useState, useEffect} from 'react';

// import axios from "axios"; 

import './Dashboard.css';

import { SpreadshMetaModel } from '../../model/model';
import SpreadsheetNodeList from './component/nodes';
import ImportPopup from './component/file_upload';

import { load_all_nodes } from './loadNodes';
import ProfileCircle from './component/profile_dropdown';

import { api_url } from '../../utilities/api';
import Button from 'react-bootstrap/esm/Button';
import AppIcon from '../../components/AppIcon';
import { longPollSyncWhile } from '../../utilities/poll';

const Dashboard:React.FunctionComponent = () => {

  const [importPopupBool, setImportPopupBool] = useState<boolean>(false);
  const [smodels, setSmodels] = useState<SpreadshMetaModel[]>([]);

  async function startDashboardLongPoll() {
    // eslint-disable-next-line no-constant-condition
    await longPollSyncWhile("/dashboard", `${api_url}/api/sync/user`, "dashboard_sync", longPollCallback);
  }

  function longPollCallback() {
    load_all_nodes(setSmodels);
  }

  useEffect(() => {
    console.log("start up");
    load_all_nodes(setSmodels);
    startDashboardLongPoll();
  }, [])
  
  const newNode = (e:React.FormEvent) => {
    e.preventDefault();

    setImportPopupBool(true);
  }

  const newDemoNode = (e: React.FormEvent) => {
    e.preventDefault();

    fetch(`${api_url}/api/spreadsheet/demo`, {
      method: "POST",
      credentials: "include"
    }).then((resolved) => {
      console.log("demofile success" + resolved);
      load_all_nodes(setSmodels);
    },
    (rejected: Error) => {
      console.log("demofile REJECTED");
      console.log(rejected.message);
    });
  }

  const [testAPIMsg, setTestAPIMsg] = useState<string>("")

  const testAPI = () => {
    fetch(`${api_url}/api/WeatherForecast`, {
      method: "GET",
      credentials: 'include'
    })
    .then((res) => {
      return res.json();
    })
    .then((data) => {
      console.log(data);
      setTestAPIMsg("api works!")
    }).catch(error => {
      console.log(error)
      setTestAPIMsg("api error")
    })
  }

  return (
    <div className="Dashboard">
      <div className='TopBar'>

        <AppIcon/>

        <div className='ProfileDiv'>
          <ProfileCircle />
        </div>
      </div>

      <div className="MainPanel">
        
        <div className="SidePanel">
          <Button variant="light" id="new-button" onClick={(e) => newNode(e)}>+ New</Button>
          <Button variant="light" id="new-demo-button" onClick={(e) => newDemoNode(e)}>+ Demo spreadsheet</Button>
          <Button variant="light" id="api-test-button" onClick={testAPI}>Test API connection</Button>
          <p>{testAPIMsg}</p>
        </div>

        <div className="MySpreadsheets">
          <h3>My Spreadsheets</h3>
          <div className="SpreadsheetDiv">
            <SpreadsheetNodeList smodels={smodels} setSmodels={setSmodels}/>
          </div>
        </div>
      </div>

      <ImportPopup importPopupBool={importPopupBool} setImportPopupBool={setImportPopupBool} setSmodels={setSmodels}/>
    </div>
  );
}

export default Dashboard;
