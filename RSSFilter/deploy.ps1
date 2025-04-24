If (Test-Path "RSSFilter.tar.gz") { Remove-Item "RSSFilter.tar.gz" }
tar -czvf RSSFilter.tar.gz *
scp RSSFilter.tar.gz anthony@192.168.1.2:/home/anthony