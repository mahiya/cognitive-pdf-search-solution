using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Linq;

namespace Functions
{
    class OcredPdfDocument
    {
        public IEnumerable<Page> Pages { get; set; }

        public static OcredPdfDocument ParseAnalyzeResult(AnalyzeResults analyzeResult)
        {
            var document = new OcredPdfDocument();
            document.Pages = analyzeResult.ReadResults.Select(r =>
            {
                // Azure Cognitive Service - Computer Vision の Read オペレーションが
                // 各ページのテキストを行など関係なく抽出するため、
                // 抽出したテキストの位置情報(BoundingBox)から、
                // 「同じ行のテキストかどうか」「隣と距離が遠いテキストかどうか」を判定している
                // 別の行のテキストは別の行のテキストとして扱い、隣と距離が遠いテキストの場合はスペースを挟めて結合している

                // 前テキストの右側のX軸の位置 (初期値は１つ目のテキストの位置情報を使用)
                var prexRightX = r.Lines.Any() ? GetRightX(r.Lines.First().BoundingBox) : 0;

                // 全テキストのY軸の位置 (初期値は１つ目のテキストの位置情報を使用)
                var prevY = r.Lines.Any() ? GetY(r.Lines.First().BoundingBox) : 0;

                var lines = new List<string>();
                var line = "";
                foreach (var text in r.Lines)
                {
                    // 前テキストとの垂直方向の距離(前テキストY軸と現テキストY軸の差分)を計算
                    var y = GetY(text.BoundingBox);
                    var diffY = y - prevY;
                    prevY = y;

                    // 垂直方向の距離差がある場合、別の行のテキストとして処理する
                    if (diffY > 0.1)
                    {
                        lines.Add(line);
                        line = "";
                    }

                    // 前テキストとの水平方向の距離(前テキスト右側X軸と現テキスト左側X軸の差分)を計算
                    var diffX = GetLeftX(text.BoundingBox) - prexRightX;
                    prexRightX = GetRightX(text.BoundingBox);

                    // 水平方向の距離差がある場合、スペースを挟めてテキストを結合する
                    if (diffX > 0.3 && line.Length > 0)
                    {
                        line += " " + text.Text;
                    }
                    else
                    {
                        // 距離差がない場合はスペースを挟めないでテキストを結合する
                        line += text.Text;
                    }
                }
                lines.Add(line); // 最終行の処理
                return new Page
                {
                    PageNumber = r.Page,
                    Text = string.Join(" ", lines),
                    Lines = lines,
                };
            });
            return document;
        }

        static double GetRightX(IList<double?> boundingBox)
        {
            return GetBoundingBoxAverageValue(boundingBox, 2, 4);
        }

        static double GetLeftX(IList<double?> boundingBox)
        {
            return GetBoundingBoxAverageValue(boundingBox, 0, 6);
        }

        static double GetY(IList<double?> boundingBox)
        {
            return GetBoundingBoxAverageValue(boundingBox, 1, 3, 5, 7);
        }

        static double GetBoundingBoxAverageValue(IList<double?> boundingBox, params int[] indexes)
        {
            return indexes.Where(i => boundingBox[i].HasValue).Select(i => boundingBox[i].Value).Average();
        }

        public class Page
        {
            public int PageNumber { get; set; }
            public string Text { get; set; }
            public List<string> Lines { get; set; }
        }
    }
}