const settings = {
    "name": "{{NAME}}",
    "key": "{{KEY}}",
    "indexName": "{{INDEX_NAME}}",
    "apiVersion": "2020-06-30",
    "searchTop": 5,
    "select": "blobName,pageNumber,text",
    "highlight": "text",
    "highlightPreTag": "<span class='bg-warning'>",
    "highlightPostTag": "</span>",
    "facetNames": {
        "blobName": "ファイル名"
    },
    "suggesterName": "sg",
    "suggestionTop": 5
};