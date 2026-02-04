# stash notes features

Stash notes are quick notes organised in catgeories
each category is a single mardown file containing many stash note.


## storage

### path

stash notes files are stored in the repository root under the stashes directory

### categories structure

a category is a single markdown file.

the file structure is as follow

 **a front matter header**
```markdown
---
id: <unique generated technical id>
title: <title>
desc: <description of the category>
updated: <last update timestamp>
created: <creation timestamp>
---
```
Where :
 - id : is a technical gererated id
 - title : a short title , will be used as category file name
 - description : a longer category description (used for display). optional, cateogory title will be used if missing.
 - updated : last update timestamp
 - created : creation timestamp

**a list of stash notes**
notes are simple markdown content separated by a markdown separator
```markdown
______________ 
```

if note starts with level 1 header (single hash `#`) then this is the title of the note


## back end

back end should propose endpoints for accessing , modifying, adding stash notes
endpoints are prefixed by `/stash`.
* categories 
  * GET /stash/categories : returns a list of available categories (the DTO mus convey id,title,description, notes count)
  * POST /stash/category: create a new category i.e. a new category file. POSt request body should convey title and description (optional)
  * PUT /stash/catgeory/{category_id} : update the category with category_id(either title or category or both)
* stash notes
  * GET /stash/categories/{category_id} : get the list of notes in the category {catgeory_id}. a note DTO is (content, title if available, some identification)
  * POST /stash/categories/{category_id} : add a new note with a content in category {category_id}.POST body is the note content 
  * PUT /stash/categories/{categories_id}/note/{note_id} : update note {note_id} in category {category_id}
  * DELETE /stash/categories/{categories_id}/note/{note_id} : remove note {note_id} from category {category_id}

**opened question** 
we need to find a way to identify notes with a {note_id} forPUT and DELETE endpoints.
this should not add technical data in the note content. 

## front end

will be specified later.



## front end

for front end : add an entry in the burger menu "stashes"
the stash link leads to a new pages that :
 - provides a way to choose a category (if any exist)
 - provides a way to create a category
 

when selecting an existing category : 
 - provide a way to add a new note
 - provide a search component to search in stashes => filter stashes accordingly
 - display the list as an list of accordions
 - display a clickable list of categories using the category description as label. default to note title if description is missing
 - when clicking a list item, deploy the accordion item and display the markdown (formatted)
 - add a pen icon upper right of the note to allow edition
   -  when clicked change the note preview to textarea to edit note + add a save icon somewhere
 - add a delete button next to edit button


# caching

for performance caching we need some caching to avoid round trip to github repository as it is already the case for classical dendron notes.

## asp.net session cache

First, stash notes (and their catgeory) MUST be stored in asp.net session. if categories and stashes are in session avoid reload from github

## front end session

categories and stashes must be stored client side in the localstorage.

## consistency

obviously consistency between versions (front , back session and github) must be preserved.

