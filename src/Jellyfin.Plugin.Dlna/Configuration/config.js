const DlnaConfigurationPage = {
    pluginUniqueId: '33EBA9CD-7DA1-4720-967F-DD7DAE7B74A1',
    defaultDiscoveryInterval: 60,
    defaultAliveInterval: 100,
    loadConfiguration: function (page) {
        ApiClient.getPluginConfiguration(this.pluginUniqueId)
            .then(function(config) {
                page.querySelector('#dlnaPlayTo').checked = config.EnablePlayTo;
                page.querySelector('#dlnaDiscoveryInterval').value = parseInt(config.ClientDiscoveryIntervalSeconds) || this.defaultDiscoveryInterval;
                page.querySelector('#dlnaBlastAlive').checked = config.BlastAliveMessages;
                page.querySelector('#dlnaAliveInterval').value = parseInt(config.AliveMessageIntervalSeconds) || this.defaultAliveInterval;
                page.querySelector('#dlnaMatchedHost').checked = config.SendOnlyMatchedHost;

                ApiClient.getUsers()
                    .then(function(users){
                        DlnaConfigurationPage.populateUsers(page, users, config.DefaultUserId);
                    })
                    .finally(function (){
                        Dashboard.hideLoadingMsg();
                    });
            });
    },
    populateUsers: function(page, users, selectedId){
        let html = '';
        html += '<option value="">None</option>';
        for(let i = 0, length = users.length; i < length; i++) {
            const user = users[i];
            html += '<option value="' + user.Id + '">' + user.Name + '</option>';
        }
        
        page.querySelector('#dlnaSelectUser').innerHTML = html;
        page.querySelector('#dlnaSelectUser').value = selectedId;
    },
    save: function(page) {
        Dashboard.showLoadingMsg();
        return new Promise((_) => {
            ApiClient.getPluginConfiguration(this.pluginUniqueId)
                .then(function(config) {
                    config.EnablePlayTo = page.querySelector('#dlnaPlayTo').checked;
                    config.ClientDiscoveryIntervalSeconds = parseInt(page.querySelector('#dlnaDiscoveryInterval').value) || this.defaultDiscoveryInterval;
                    config.BlastAliveMessages = page.querySelector('#dlnaBlastAlive').checked;
                    config.AliveMessageIntervalSeconds = parseInt(page.querySelector('#dlnaAliveInterval').value) || this.defaultAliveInterval;
                    config.SendOnlyMatchedHost = page.querySelector('#dlnaMatchedHost').checked;
                    
                    let selectedUser = page.querySelector('#dlnaSelectUser').value;
                    config.DefaultUserId = selectedUser.length > 0 ? selectedUser : null;

                    ApiClient.updatePluginConfiguration(DlnaConfigurationPage.pluginUniqueId, config).then(Dashboard.processPluginConfigurationUpdateResult);
                });
        })
    }
}

export default function(view) {
    view.querySelector('#dlnaForm').addEventListener('submit', function(e) {
        DlnaConfigurationPage.save(view);
        e.preventDefault();
        return false;
    });
    
    window.addEventListener('pageshow', function(_) {
        Dashboard.showLoadingMsg();
        DlnaConfigurationPage.loadConfiguration(view);
    });
}