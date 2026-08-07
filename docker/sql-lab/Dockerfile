FROM mcr.microsoft.com/mssql/server@sha256:ba4c8329f48fb8f02e1416be6a930ebfd71268caee78aa985f3af4315e457c89

USER root
RUN setcap -r /opt/mssql/bin/sqlservr
COPY --chmod=0555 entrypoint.sh /usr/local/bin/forge-sql-entrypoint
COPY --chmod=0444 tcp-bridge.py /usr/local/bin/sql-lab-bridge.py

USER 10001:0
ENTRYPOINT ["/bin/bash", "/usr/local/bin/forge-sql-entrypoint"]
