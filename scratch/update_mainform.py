with open('MainForm.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = """if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv)
                {
                    mdv.SelectTab("Routes");
                    var routesView = mdv.GetDeliveryRoutesView();
                    routesView?.OpenForShortcut(routeCode, isNewRoute, returnTarget, backAction);
                }"""
replacement = """if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv)
                {
                    mdv.SelectTab("Categories");
                    var catView = mdv.GetCategoryManagementView();
                    catView?.OpenForRouteShortcut(routeCode, isNewRoute, returnTarget, backAction);
                }"""

if target in text:
    text = text.replace(target, replacement, 1)
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), replacement.replace('\n', '\r\n'), 1)
else:
    raise Exception('target not found')

with open('MainForm.cs', 'w', encoding='utf-8') as f:
    f.write(text)
print('MainForm.cs updated!')
