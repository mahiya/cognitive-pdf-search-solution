new Vue({
    el: '#app',
    data: {
        cognitiveSearch: {},
        search: "",
        searching: false,
        docs: [],
        docsCount: null,
        suggestions: [],
        facets: {},
        checkedFacets: {},
        page: 1,
        maxPage: null,
        functionApi: null,
        displayingPdfUrl: null
    },
    // 画面表示時の処理
    async mounted() {
        this.cognitiveSearch = settings.cognitiveSearch;
        this.functionApi = settings.publishTempPdfUrlApiEndpoint;
        await this.searchDocuments();
    },
    watch: {
        // 検索テキストボックスの値が変更された時の処理
        search: function () {
            this.page = 1;
            this.maxPage = null;
            this.docsCount = null;
            this.getSuggestions();
            this.searchDocuments();
        },
        // ファセットのチェックが変更された時の処理
        checkedFacets: {
            handler: function () {
                this.searchDocuments();
            },
            deep: true
        }
    },
    methods: {
        // 検索結果を取得する
        searchDocuments: async function () {
            this.docs = [];
            this.searching = true;

            const url = `https://${this.cognitiveSearch.name}.search.windows.net/indexes/${this.cognitiveSearch.indexName}/docs/search?api-version=${this.cognitiveSearch.apiVersion}`;
            const headers = {
                "Content-Type": "application/json",
                "api-key": this.cognitiveSearch.key
            };
            const body = {
                search: this.search,
                top: this.cognitiveSearch.searchTop,
                skip: (this.page - 1) * this.cognitiveSearch.searchTop,
                facets: Object.keys(this.cognitiveSearch.facetNames).map(
                    (f) => `${f},count:5,sort:count`
                ),
                count: true,
                highlight: this.cognitiveSearch.highlight,
                highlightPreTag: this.cognitiveSearch.highlightPreTag,
                highlightPostTag: this.cognitiveSearch.highlightPostTag,
            };
            if (this.cognitiveSearch.scoringProfile)
                body["scoringProfile"] = settings.scoringProfile;

            // ファセットでのフィルタを定義する
            const filters = [];
            for (const facetValue of Object.keys(this.checkedFacets)) {
                if (this.checkedFacets[facetValue]) {
                    const splited = facetValue.split('%%%%%');
                    const facet = splited[0];
                    const value = splited[1];
                    filters.push(`${facet} eq '${value}'`);
                }
            }
            if (filters.length > 0) {
                body['filter'] = `(${filters.join(" and ")})`;
            }

            // REST API の呼び出し
            const resp = await axios.post(url, body, { headers });

            // 検索結果のうち、検索キーワードの箇所をハイライトする
            this.searching = false;
            this.docs = resp.data.value.map(value => {
                if (value["@search.highlights"]) {
                    for (const highlightProp of Object.keys(value["@search.highlights"])) {
                        for (const highlight of value["@search.highlights"][highlightProp]) {
                            const replaceFrom = highlight.replaceAll(this.cognitiveSearch.highlightPreTag, "").replaceAll(this.cognitiveSearch.highlightPostTag, "");
                            const replaceTo = highlight;
                            value[highlightProp] = value[highlightProp].replaceAll(replaceFrom, replaceTo);
                        }
                    }
                }
                return value;
            });

            // ファセット情報を取得する
            const facets = resp.data["@search.facets"];
            if (facets) {
                this.facets = Object.keys(facets).map(facetName => {
                    return {
                        name: facetName,
                        displayName: this.cognitiveSearch.facetNames[facetName],
                        values: facets[facetName].filter(f => f.value).map(f => {
                            return {
                                id: `${facetName}%%%%%${f.value}`,
                                displayName: f.value,
                                count: f.count,
                            };
                        })
                    };
                });
            }

            // 検索対象のデータ数を取得する
            this.docsCount = resp.data["@odata.count"];
            this.maxPage = Math.ceil(this.docsCount / this.cognitiveSearch.searchTop);

            // 検索結果のトップの PDF ファイルを画面に表示する
            if (this.docs.length > 0) {
                await this.displayPdfFile(this.docs[0]);
            }
        },
        // サジェストを取得する
        getSuggestions: async function () {

            // 検索フィールドの入力がない場合は処理を行わない
            if (!this.search) return;

            // サジェスター名が指定されていない場合は処理を行わない
            if (!this.cognitiveSearch.suggesterName) return;

            // Azure Cognitive Search REST API を呼び出してサジェストを取得する
            // 参考：https://learn.microsoft.com/ja-jp/rest/api/searchservice/suggestions
            const queryParameters = {
                "api-version": this.cognitiveSearch.apiVersion,
                "search": this.search,
                "suggesterName": this.cognitiveSearch.suggesterName,
                "$top": 50,
            };
            const queryString = Object.keys(queryParameters).map(key => [key, queryParameters[key]].join("=")).join("&");
            const url = `https://${this.cognitiveSearch.name}.search.windows.net/indexes/${this.cognitiveSearch.indexName}/docs/suggest?${queryString}`;
            const headers = {
                "Content-Type": "application/json",
                "api-key": this.cognitiveSearch.key
            };
            const resp = await axios.get(url, { headers });

            // サジェストの重複を排除する
            const values = []
            for (const value of resp.data.value) {
                if (!values.includes(value["@search.text"]))
                    values.push(value["@search.text"]);
                if (values.length >= this.cognitiveSearch.suggestionTop) break;
            }
            this.suggestions = values;
        },
        // ページネーションがクリックされた時の処理
        onPagenationClicked: function (newPage) {
            this.page = newPage;
            this.searchDocuments();
        },
        // 指定した検索結果のPDFファイル(ページ指定)を画面に表示する
        displayPdfFile: async function (doc) {
            const url = `${this.functionApi}&n=${encodeURI(doc.blobName)}`;
            const resp = await axios.get(url);
            this.displayingPdfUrl = `${resp.data}#page=${doc.pageNumber}`;
        },
    }
});

