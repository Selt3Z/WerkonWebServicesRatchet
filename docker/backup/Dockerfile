FROM postgres:17-alpine

RUN apk add --no-cache bash tzdata restic

COPY backup.sh /usr/local/bin/backup.sh
COPY restore-test.sh /usr/local/bin/restore-test.sh
RUN chmod +x /usr/local/bin/backup.sh /usr/local/bin/restore-test.sh

CMD ["/usr/local/bin/backup.sh"]
