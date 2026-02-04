# logout

## description

Add a button to allow user to logout.
button must be accessible any time in the burger menu
When logged out, user need to relogged using github oauth to access his notes

## technical requirements

when logging out : 
 - revoke session cookies
 - remove cache items from localstorage, but keep favorite repo id
 - revoke the gh token so that the user needs to log again with gh to come back to dendronline

 
