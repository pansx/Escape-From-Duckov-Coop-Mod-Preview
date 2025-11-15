// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System.Net;
using System.Text;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 调试监控页面端点 "/debug"
/// </summary>
public class DebugEndpoint : IHttpEndpoint
{
    public string Path => "/debug";

    public void Handle(HttpListenerContext context)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>");
        sb.Append("<html lang='zh-CN'>");
        sb.Append("<head>");
        sb.Append("<meta charset='UTF-8'>");
        sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.Append("<title>API 监控面板</title>");
        sb.Append("<style>");
        sb.Append("* { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.Append("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; padding: 20px; }");
        sb.Append(".container { max-width: 1400px; margin: 0 auto; }");
        sb.Append(".header { text-align: center; color: white; margin-bottom: 30px; }");
        sb.Append(".header h1 { font-size: 2.5em; margin-bottom: 10px; text-shadow: 2px 2px 4px rgba(0,0,0,0.3); }");
        sb.Append(".header .status { font-size: 1.1em; opacity: 0.9; }");
        sb.Append(".cards-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(350px, 1fr)); gap: 20px; margin-bottom: 20px; }");
        sb.Append(".card { background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); overflow: hidden; transition: transform 0.2s, box-shadow 0.2s; }");
        sb.Append(".card:hover { transform: translateY(-5px); box-shadow: 0 8px 12px rgba(0,0,0,0.2); }");
        sb.Append(".card-header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 20px; cursor: pointer; user-select: none; }");
        sb.Append(".card-header:hover { opacity: 0.95; }");
        sb.Append(".card-title { font-size: 1.3em; font-weight: bold; margin-bottom: 8px; }");
        sb.Append(".card-stats { display: flex; justify-content: space-between; font-size: 0.9em; opacity: 0.95; }");
        sb.Append(".card-body { padding: 20px; }");
        sb.Append(".stat-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; }");
        sb.Append(".stat-row:last-child { border-bottom: none; }");
        sb.Append(".stat-label { font-weight: 600; color: #555; }");
        sb.Append(".stat-value { color: #667eea; font-weight: bold; }");
        sb.Append(".json-container { display: none; margin-top: 15px; padding: 15px; background: #f8f9fa; border-radius: 8px; border: 1px solid #dee2e6; }");
        sb.Append(".json-container.expanded { display: block; }");
        sb.Append(".json-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }");
        sb.Append(".json-title { font-weight: bold; color: #333; }");
        sb.Append(".copy-btn { background: #667eea; color: white; border: none; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 0.9em; transition: background 0.2s; }");
        sb.Append(".copy-btn:hover { background: #5568d3; }");
        sb.Append(".copy-btn.copied { background: #28a745; }");
        sb.Append(".json-content { background: #2d2d2d; color: #f8f8f2; padding: 15px; border-radius: 6px; overflow-x: auto; font-family: 'Courier New', monospace; font-size: 0.9em; line-height: 1.5; max-height: 400px; overflow-y: auto; }");
        sb.Append(".loading { text-align: center; padding: 20px; color: #999; }");
        sb.Append(".error { background: #f8d7da; color: #721c24; padding: 15px; border-radius: 8px; border: 1px solid #f5c6cb; }");
        sb.Append(".expand-icon { display: inline-block; transition: transform 0.3s; margin-right: 8px; }");
        sb.Append(".expand-icon.expanded { transform: rotate(90deg); }");
        sb.Append(".log-download-section { text-align: center; margin-bottom: 30px; }");
        sb.Append(".download-btn { display: inline-block; background: white; color: #667eea; padding: 15px 30px; border-radius: 10px; text-decoration: none; font-size: 1.1em; font-weight: bold; box-shadow: 0 4px 6px rgba(0,0,0,0.1); transition: all 0.3s; }");
        sb.Append(".download-btn:hover { transform: translateY(-3px); box-shadow: 0 6px 12px rgba(0,0,0,0.2); background: #f8f9fa; }");
        sb.Append("@media (max-width: 768px) { .cards-grid { grid-template-columns: 1fr; } .header h1 { font-size: 1.8em; } }");
        sb.Append("</style>");
        sb.Append("</head>");
        sb.Append("<body>");
        sb.Append("<div class='container'>");
        sb.Append("<div class='header'>");
        sb.Append("<h1>&#127918; API 监控面板</h1>");
        sb.Append("<div class='status'>实时监控 &middot; 每 3 秒自动刷新</div>");
        sb.Append("</div>");
        sb.Append("<div class='log-download-section'>");
        sb.Append("<a href='/api/log' download='latest.log' class='download-btn'>");
        sb.Append("&#128190; 下载游戏日志 (latest.log)");
        sb.Append("</a>");
        sb.Append("</div>");
        sb.Append("<div class='cards-grid' id='cardsGrid'>");
        sb.Append("<div class='loading'>正在加载数据...</div>");
        sb.Append("</div>");
        sb.Append("</div>");
        
        // JavaScript
        sb.Append("<script>");
        sb.Append("const endpoints = [");
        sb.Append("{ path: '/api/players', name: '玩家列表' },");
        sb.Append("{ path: '/api/ais', name: 'AI 列表' },");
        sb.Append("{ path: '/api/lootboxes', name: '战利品箱' },");
        sb.Append("{ path: '/api/vote', name: '投票状态' }");
        sb.Append("];");
        sb.Append("const endpointData = {};");
        
        sb.Append("async function fetchEndpoint(endpoint) {");
        sb.Append("try {");
        sb.Append("const response = await fetch(endpoint.path);");
        sb.Append("const data = await response.json();");
        sb.Append("endpointData[endpoint.path] = { success: true, data: data, timestamp: new Date().toLocaleString('zh-CN'), count: getDataCount(data) };");
        sb.Append("} catch (error) {");
        sb.Append("endpointData[endpoint.path] = { success: false, error: error.message, timestamp: new Date().toLocaleString('zh-CN') };");
        sb.Append("}");
        sb.Append("}");
        
        sb.Append("function getDataCount(data) {");
        sb.Append("if (data.count !== undefined) return data.count;");
        sb.Append("if (data.statistics && data.statistics.readyPlayers !== undefined && data.statistics.totalPlayers !== undefined) {");
        sb.Append("return data.statistics.readyPlayers + '/' + data.statistics.totalPlayers;");
        sb.Append("}");
        sb.Append("if (data.players) return data.players.length;");
        sb.Append("if (data.ais) return data.ais.length;");
        sb.Append("if (data.lootboxes) return data.lootboxes.length;");
        sb.Append("if (Array.isArray(data)) return data.length;");
        sb.Append("return '-';");
        sb.Append("}");
        
        sb.Append("function getLastItem(data) {");
        sb.Append("if (data.players && data.players.length > 0) return data.players[data.players.length - 1];");
        sb.Append("if (data.ais && data.ais.length > 0) return data.ais[data.ais.length - 1];");
        sb.Append("if (data.lootboxes && data.lootboxes.length > 0) return data.lootboxes[data.lootboxes.length - 1];");
        sb.Append("if (Array.isArray(data) && data.length > 0) return data[data.length - 1];");
        sb.Append("return null;");
        sb.Append("}");
        
        sb.Append("let isFirstRender = true;");
        
        sb.Append("function renderCards() {");
        sb.Append("const grid = document.getElementById('cardsGrid');");
        sb.Append("if (isFirstRender) {");
        sb.Append("grid.innerHTML = '';");
        sb.Append("endpoints.forEach(endpoint => {");
        sb.Append("const cardId = endpoint.path.replace(/\\//g, '-');");
        sb.Append("const card = document.createElement('div');");
        sb.Append("card.className = 'card';");
        sb.Append("card.id = 'card-' + cardId;");
        sb.Append("let html = '<div class=\"card-header\" onclick=\"toggleJson(\\'' + cardId + '\\')\"><div class=\"card-title\"><span class=\"expand-icon\" id=\"icon-' + cardId + '\">&#9654;</span>' + endpoint.name + '</div><div class=\"card-stats\"><span id=\"count-' + cardId + '\">数量: -</span><span id=\"time-' + cardId + '\">-</span></div></div>';");
        sb.Append("html += '<div class=\"card-body\">';");
        sb.Append("html += '<div class=\"stat-row\"><span class=\"stat-label\">端点路径</span><span class=\"stat-value\">' + endpoint.path + '</span></div>';");
        sb.Append("html += '<div class=\"stat-row\"><span class=\"stat-label\">数据条数</span><span class=\"stat-value\" id=\"count2-' + cardId + '\">-</span></div>';");
        sb.Append("html += '<div class=\"stat-row\"><span class=\"stat-label\">更新时间</span><span class=\"stat-value\" id=\"time2-' + cardId + '\">-</span></div>';");
        sb.Append("html += '<div class=\"stat-row\" id=\"lastitem-row-' + cardId + '\" style=\"display:none;\"><span class=\"stat-label\">最后一条</span><span class=\"stat-value\" id=\"lastitem-' + cardId + '\">-</span></div>';");
        sb.Append("html += '<div class=\"json-container\" id=\"json-' + cardId + '\"><div class=\"json-header\"><span class=\"json-title\">完整 JSON 数据</span><button class=\"copy-btn\" onclick=\"copyJson(\\'' + cardId + '\\', event)\">复制</button></div><pre class=\"json-content\" id=\"content-' + cardId + '\"></pre></div>';");
        sb.Append("html += '</div>';");
        sb.Append("card.innerHTML = html;");
        sb.Append("grid.appendChild(card);");
        sb.Append("});");
        sb.Append("isFirstRender = false;");
        sb.Append("}");
        sb.Append("endpoints.forEach(endpoint => {");
        sb.Append("const cardId = endpoint.path.replace(/\\//g, '-');");
        sb.Append("const epData = endpointData[endpoint.path];");
        sb.Append("const cardElement = document.getElementById('card-' + cardId);");
        sb.Append("if (!epData) return;");
        sb.Append("if (endpoint.path === '/api/vote' && epData.success && epData.data.active === false) {");
        sb.Append("if (cardElement) cardElement.style.display = 'none';");
        sb.Append("return;");
        sb.Append("}");
        sb.Append("if (cardElement) cardElement.style.display = 'block';");
        sb.Append("if (epData.success) {");
        sb.Append("const lastItem = getLastItem(epData.data);");
        sb.Append("const activeStatus = epData.data.active !== undefined ? (epData.data.active ? ' (进行中)' : ' (无投票)') : '';");
        sb.Append("document.getElementById('count-' + cardId).textContent = '数量: ' + epData.count + activeStatus;");
        sb.Append("document.getElementById('time-' + cardId).textContent = epData.timestamp;");
        sb.Append("document.getElementById('count2-' + cardId).textContent = epData.count;");
        sb.Append("document.getElementById('time2-' + cardId).textContent = epData.timestamp;");
        sb.Append("const lastItemRow = document.getElementById('lastitem-row-' + cardId);");
        sb.Append("if (lastItem) {");
        sb.Append("lastItemRow.style.display = 'flex';");
        sb.Append("document.getElementById('lastitem-' + cardId).textContent = getLastItemPreview(lastItem);");
        sb.Append("} else {");
        sb.Append("lastItemRow.style.display = 'none';");
        sb.Append("}");
        sb.Append("document.getElementById('content-' + cardId).textContent = JSON.stringify(epData.data, null, 2);");
        sb.Append("}");
        sb.Append("});");
        sb.Append("}");
        
        sb.Append("function getLastItemPreview(item) {");
        sb.Append("if (item.name) return item.name;");
        sb.Append("if (item.id) return 'ID: ' + item.id;");
        sb.Append("if (item.instanceId) return '实例: ' + item.instanceId;");
        sb.Append("return '查看详情';");
        sb.Append("}");
        
        sb.Append("function toggleJson(cardId) {");
        sb.Append("const container = document.getElementById('json-' + cardId);");
        sb.Append("const icon = document.getElementById('icon-' + cardId);");
        sb.Append("if (container.classList.contains('expanded')) {");
        sb.Append("container.classList.remove('expanded');");
        sb.Append("icon.classList.remove('expanded');");
        sb.Append("} else {");
        sb.Append("container.classList.add('expanded');");
        sb.Append("icon.classList.add('expanded');");
        sb.Append("}");
        sb.Append("}");
        
        sb.Append("function copyJson(cardId, event) {");
        sb.Append("event.stopPropagation();");
        sb.Append("const content = document.getElementById('content-' + cardId).textContent;");
        sb.Append("const btn = event.target;");
        sb.Append("navigator.clipboard.writeText(content).then(() => {");
        sb.Append("const originalText = btn.textContent;");
        sb.Append("btn.textContent = '已复制!';");
        sb.Append("btn.classList.add('copied');");
        sb.Append("setTimeout(() => { btn.textContent = originalText; btn.classList.remove('copied'); }, 2000);");
        sb.Append("}).catch(err => { alert('复制失败: ' + err); });");
        sb.Append("}");
        
        sb.Append("async function refreshData() {");
        sb.Append("await Promise.all(endpoints.map(ep => fetchEndpoint(ep)));");
        sb.Append("renderCards();");
        sb.Append("}");
        
        sb.Append("refreshData();");
        sb.Append("setInterval(refreshData, 3000);");
        sb.Append("</script>");
        
        sb.Append("</body>");
        sb.Append("</html>");

        HttpHelper.SendHtml(context.Response, sb.ToString());
    }
}
