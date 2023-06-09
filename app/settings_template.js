const settings = {
    cognitiveSearch: {
        name: "{{COGNITIVE_SEARCH_NAME}}",
        key: "{{COGNITIVE_SEARCH_KEY}}",
        indexName: "{{COGNITIVE_SEARCH_INDEX_NAME}}",
        apiVersion: "2020-06-30",
        searchTop: 5,
        select: "blobName,pageNumber,text",
        highlight: "text",
        highlightPreTag: "<span class='bg-warning'>",
        highlightPostTag: "</span>",
        facetNames: {
            blobName: "ファイル名"
        },
        suggesterName: "sg",
        suggestionTop: 5,
    },
    publishTempPdfUrlApiEndpoint: "{{FUNCTION_API_ENDPOINT}}"
};