<script setup lang="ts">
import { computed } from 'vue'
import { formatDate } from '@/utils/format'
import { isHttpUrl } from '@/validation/httpUrl'

const props = defineProps<{
  userName: string
  email: string
  homePage: string | null
  createdAt: string
}>()

const safeHomePage = computed(() => (props.homePage && isHttpUrl(props.homePage) ? props.homePage : null))
</script>

<template>
  <div class="meta">
    <span class="meta-name">
      <a v-if="safeHomePage" :href="safeHomePage" target="_blank" rel="noopener noreferrer nofollow">{{ userName }}</a>
      <template v-else>{{ userName }}</template>
    </span>
    <span class="meta-email">{{ email }}</span>
    <span class="meta-date">{{ formatDate(createdAt) }}</span>
  </div>
</template>
