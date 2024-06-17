# Data Cleansing App
Student project to build a full-stack web application hosted in the cloud.
Allows users to perform data cleansing operations such as data deduplication and collaborative editing on uploaded spreadsheets.

Features include:
- username & password login using cookie-based authentication
- upload XML spreadsheet file
- dashboard to organize uploaded spreadsheets
- able to share spreadsheets with other users
- server push events using long polling
- see edits by other users on a shared spreadsheet in real-time
- run a deterministic data deduplication algorithm to clean spreadsheet data
- create validation checkboxes for collaborative manual validation

## Screenshots

Landing page

<img width="505" alt="Screenshot 2024-05-06 131514" src="https://github.com/shibaholic/datacleansingapp/assets/148887683/2777b3bc-3d33-4862-8fe7-398de623615b">

Dashboard

<img width="506" alt="Screenshot 2024-05-06 131520" src="https://github.com/shibaholic/datacleansingapp/assets/148887683/e8cb03f9-4dde-4206-a1dd-8de699f6e3fd">

Spreadsheet view with right panel open

<img width="505" alt="Screenshot 2024-05-06 131606 2" src="https://github.com/shibaholic/datacleansingapp/assets/148887683/857296f2-5947-46d0-89e4-3d0a6d0a4da9">

## Architecture and technologies
![High-level technologies](https://github.com/shibaholic/datacleansingapp/assets/148887683/58305886-fd32-4f7a-8c1d-f7d2f15c367a)

## Database Entity Relationship Diagram
![entity relationship diagram](https://github.com/shibaholic/datacleansingapp/assets/148887683/99e9a301-03ce-4996-8e91-518f4d7c0014)

Since the spreadsheets would have an unknown amount of columns and rows...
