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
<p>
  <img width="505" alt="Screenshot 2024-05-06 131514" src="https://github.com/shibaholic/datacleansingapp/assets/148887683/2777b3bc-3d33-4862-8fe7-398de623615b">
</p>



Dashboard
<p>
<img width="506" alt="Screenshot 2024-05-06 131520" src="https://github.com/shibaholic/datacleansingapp/assets/148887683/e8cb03f9-4dde-4206-a1dd-8de699f6e3fd">
</p>



Spreadsheet view with right panel open
<p>
<img width="505" alt="Screenshot 2024-05-06 131606 2" src="https://github.com/shibaholic/datacleansingapp/assets/148887683/857296f2-5947-46d0-89e4-3d0a6d0a4da9">
</p>

## Architecture and technologies
![High-level technologies](https://github.com/shibaholic/datacleansingapp/assets/148887683/58305886-fd32-4f7a-8c1d-f7d2f15c367a)

Dapper was used to directly code the SQL queries. 

## Database Entity Relationship Diagram
![entity relationship diagram](https://github.com/shibaholic/datacleansingapp/assets/148887683/99e9a301-03ce-4996-8e91-518f4d7c0014)

Since the spreadsheets would have an unknown amount of columns and rows (semi-structured data), a standard relational database design would probably not be able to maintain the necessary performance required as spreadsheets (with 10,000s of rows) would be stored in the same table (resulting in a massive table, which would have to be traversed for data cleansing (not good) ). 

Instead a _radical?_ approach was taken where each spreadsheet would be parsed for it's columns, from which a table with matching columns would be dynamically created. Each of these dynamically created tables would be identified by their table name which would be stored in the SpreadsheetConfig table for lookup. This approach creates more complexity for better performance.

However the ideal solution would have been to use a different database technology such as one suited for XML or semi-structured data, which would remove this complexity and keep high-performance.
