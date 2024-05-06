/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useState, useEffect } from 'react';

import Button from 'react-bootstrap/Button';
import Modal from 'react-bootstrap/Modal';
import Badge from 'react-bootstrap/Badge';
import ProgressBar from 'react-bootstrap/ProgressBar';

import axios from "axios";

import { SpreadshMetaModel } from '../../../model/model';
import { wait } from '../../../utilities/poll';
import { load_all_nodes } from '../loadNodes';
import { api_url } from '../../../utilities/api';

import './file_upload.css';
interface ImportPopupProps {
    importPopupBool:boolean;
    setImportPopupBool:React.Dispatch<React.SetStateAction<boolean>>;
    setSmodels:React.Dispatch<React.SetStateAction<SpreadshMetaModel[]>>;
}

const ImportPopup = ({importPopupBool, setImportPopupBool, setSmodels}:ImportPopupProps) => {

    const handleClose = () => { 
        setImportPopupBool(false);
    }
    
    useEffect(() => {
        setImportPopupBool(importPopupBool);
    }, [importPopupBool, setImportPopupBool])
    
    return (
        <Modal show={importPopupBool} onHide={handleClose}>
            <Modal.Header closeButton>
                <Modal.Title>Import a spreadsheet</Modal.Title>
            </Modal.Header>

            <Modal.Body>
                <FileUpload setSmodels={setSmodels} handle_close={handleClose}/>

            </Modal.Body>

        </Modal>
  );
}

interface FileUploadProps {
    handle_close: () => void,
    setSmodels:React.Dispatch<React.SetStateAction<SpreadshMetaModel[]>>
}

const FileUpload = ({handle_close, setSmodels}:FileUploadProps) => {
  const selectInputFile = (element:HTMLInputElement) => {
      setFile(element.files![0]);
  }

  // Image/File Submit Handler
  const [file, setFile] = useState<File>(new File([], ''));
  const [uploading, setUploading] = useState<boolean>(false);
  const [uploadPercent, setUploadPercent] = useState<number>(0);
  const [uploadLabel, setUploadLabel] = useState<string>("");
  const [errorMsg, setErrorMsg] = useState<string>("");

  let uploadFinished = false;

  // request that uploads the file
  async function uploadFileRequest(uploadLocation: string): Promise<number> {
    const formData = new FormData();
    formData.append('customFile', file);
    console.log(uploadLocation);

    return axios.post(
      `${api_url}${uploadLocation}`,
      formData,
      {
        headers: {
          "Content-type": "multipart/form-data",
        },
        withCredentials: true
      }).then(resolved => {
        console.log(`uploadFileRequest resolved ` + resolved.data);
        uploadFinished = true;
        return 1;
      }, rejected => {
        console.log("uploadFileRequest rejected " + rejected)
        uploadFinished = true;
        return -1;
      });
  }

  // request that checks on status of uploaded file, such as percent uploaded, uploading or complete, every 2 seconds.
  async function checkStatusRequest(uploadStatusUrl:string): Promise<number> {
    let counter = 0;
    setUploadPercent(5);
    await new Promise(resolve => setTimeout(resolve, 500));

    // eslint-disable-next-line no-constant-condition
    while (true) {
      if (uploadFinished) {
        console.log("checkStatus done");
        return 2;
      } else {
        fetch(`${api_url}${uploadStatusUrl}`, {
          method: "GET",
          credentials: "include"
        }).then((resolved) => {
          return resolved.json();
        }).then((data) => {
          console.log("checkStatusCounter " + ++counter);
          console.log(data);
          const percentOrMinimumValue = Math.max(data.value_percent, 5);
          setUploadPercent(percentOrMinimumValue);
          if (percentOrMinimumValue > 5)
            setUploadLabel(`${percentOrMinimumValue}%`);
        });
        await new Promise(resolve => setTimeout(resolve, 2_000));
      }
    }
  }

  // activated from button
  const handleSubmitFile = () => {

    console.log("file.size: " + file.size);
    if (file.size !== 0) {

      fetch(`${api_url}/api/spreadsheet/upload`, {
        method: "POST",
        credentials: "include"
      }).then((resolved) => {
        console.log("upload resolved");
        return resolved.json();
      }).then(async (data) => {
        console.log("data");
        console.log(data);
        console.log(data.sucess == true);
        if (data.success == true) {

          setUploading(true);

          await wait({ ms: 100 });

          const rawResponse = await Promise.allSettled([uploadFileRequest(data.uploadLocation), checkStatusRequest(data.uploadStatus)]);

          console.log("rawResponse");
          console.log(rawResponse);

          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          const response = rawResponse.filter((res) => res.status === "fulfilled") as PromiseFulfilledResult<any>[];
          console.log(response);

          //setUploading(false);

          console.log(response[0].value);

          if (response[0].value === 1) {
            // successfully uploaded
            console.log("upload fulfilled");
            setSuccessLabel(true);
            setUploadPercent(100);
            setUploadLabel("");
            await wait({ ms: 2000 });
            handle_close();
            load_all_nodes(setSmodels);
          } else {
            console.log("upload rejected");
            setUploading(false);
            setErrorMsg("Upload failed. Only .xlsx files are supported.");
          }
        }
      });
    }
    
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const [successLabel, setSuccessLabel] = useState<boolean>(false);

  // render from here
  return (
    <div >
      {uploading ?
        (
          <div>
            <ProgressBar animated now={uploadPercent} label={uploadLabel} />
            {successLabel ? (
              <div style={{ display: "flex", justifyContent: "center", marginTop: "1rem"}}>
                <Badge pill bg="success">Success!</Badge>
              </div>
            ) : (
                <p style={{ textAlign: "center", marginTop:"1rem" } }>uploading</p>
            )}
          </div>
        ) : (
          <div>
            <div className="fileForm">
              <input
                type="file"
                onChange={(e) => selectInputFile(e.target)}
              />
              <Button onClick={handleSubmitFile}>Submit</Button>
            </div>
            <p style={{color:"red", textAlign:"center", marginTop:"1rem"} }>{errorMsg}</p>
          </div>
        )
      }
    </div>
  );
}

export default ImportPopup;