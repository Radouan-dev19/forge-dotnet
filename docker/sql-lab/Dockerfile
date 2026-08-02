FROM mcr.microsoft.com/mssql/server@sha256:b1395aa51b4ec39981883560f1379ea9eba2a1c0719bf8e6477902769316bb79

USER root
RUN setcap -r /opt/mssql/bin/sqlservr
COPY --chmod=0555 entrypoint.sh /usr/local/bin/forge-sql-entrypoint
COPY --chmod=0444 tcp-bridge.py /usr/local/bin/sql-lab-bridge.py

USER 10001:0
ENTRYPOINT ["/bin/bash", "/usr/local/bin/forge-sql-entrypoint"]
