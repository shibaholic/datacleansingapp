import axios from 'axios';

import { SpreadshMetaModel } from '../../model/model';

import { wait } from '../../utilities/poll';
import { api_url } from '../../utilities/api';

async function load_all_nodes(setSmodels:React.Dispatch<React.SetStateAction<SpreadshMetaModel[]>>) {
    // while loop to try to fetch all nodes
    let keep_running = true;
    while(keep_running) {
        axios({
          method: 'get',
          url: `${api_url}/api/spreadsheet`,
          withCredentials: true
        }).then((data) => {
            const all_smodels:SpreadshMetaModel[] = [];
            data.data.forEach((entity:SpreadshMetaModel) => {
                all_smodels.push(entity);
            })
            setSmodels(all_smodels);
            keep_running = false;
        })
        .catch(error => {
          console.error(error);
          return;
        });
        await wait({ms:1000});
    }
} 

export { load_all_nodes };